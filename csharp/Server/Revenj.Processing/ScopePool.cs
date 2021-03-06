﻿using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.Security.Principal;
using Revenj.DatabasePersistence;
using Revenj.Extensibility;

namespace Revenj.Processing
{
	internal interface IScopePool
	{
		Scope Take(bool readOnly, IPrincipal principal);
		void Release(Scope factory, bool valid);
	}

	internal sealed class Scope
	{
		public readonly IObjectFactory Factory;
		public readonly IDatabaseQuery Query;
		public IPrincipal Principal { get; internal set; }

		public Scope(IObjectFactory factory, IDatabaseQuery query)
		{
			this.Factory = factory;
			this.Query = query;
		}
	}

	internal class ScopePool : IScopePool, IDisposable
	{
		private static readonly TraceSource TraceSource = new TraceSource("Revenj.Server");

		private readonly BlockingCollection<Scope> Scopes = new BlockingCollection<Scope>(new ConcurrentBag<Scope>());

		public enum PoolMode
		{
			None,
			Wait,
			IfAvailable
		}

		private readonly PoolMode Mode = PoolMode.IfAvailable;
		private readonly int Size;

		private readonly IObjectFactory Factory;
		private readonly IDatabaseQueryManager Queries;

		public ScopePool(
			IObjectFactory factory,
			IDatabaseQueryManager queries,
			IExtensibilityProvider extensibilityProvider)
		{
			this.Factory = factory;
			this.Queries = queries;
			if (!int.TryParse(ConfigurationManager.AppSettings["Processing.PoolSize"], out Size))
				Size = 20;
			if (!Enum.TryParse<PoolMode>(ConfigurationManager.AppSettings["Processing.PoolMode"], out Mode))
			{
				//TODO: Mono has issues with BlockingCollection. use None as default
				int p = (int)Environment.OSVersion.Platform;
				if (p == 4 || p == 6 || p == 128)
					Mode = PoolMode.None;
				else
					Mode = PoolMode.IfAvailable;
			}
			var commandTypes = extensibilityProvider.FindPlugins<IServerCommand>();
			Factory.RegisterTypes(commandTypes, InstanceScope.Context);
			if (Mode != PoolMode.None)
			{
				if (Size < 1) Size = 1;
				for (int i = 0; i < Size; i++)
					Scopes.Add(SetupReadonlyScope());
			}
		}

		private Scope SetupReadonlyScope()
		{
			var inner = Factory.CreateScope(null);
			try
			{
				var query = Queries.StartQuery(false);
				inner.RegisterInstance(query);
				return new Scope(inner, query);
			}
			catch (Exception ex)
			{
				TraceSource.TraceEvent(TraceEventType.Critical, 5301, "{0}", ex);
				inner.Dispose();
				throw;
			}
		}

		private Scope SetupWritableScope(IPrincipal principal)
		{
			var id = Guid.NewGuid().ToString();
			var inner = Factory.CreateScope(id);
			try
			{
				var query = Queries.StartQuery(true);
				inner.RegisterInstance(query);
				inner.RegisterInstance(principal);
				inner.RegisterType(typeof(ProcessingContext), InstanceScope.Singleton, typeof(IProcessingEngine));
				return new Scope(inner, query);
			}
			catch (Exception ex)
			{
				TraceSource.TraceEvent(TraceEventType.Critical, 5302, "{0}", ex);
				inner.Dispose();
				throw;
			}
		}

		public Scope Take(bool readOnly, IPrincipal principal)
		{
			if (!readOnly)
				return SetupWritableScope(principal);
			Scope scope;
			switch (Mode)
			{
				case PoolMode.None:
					scope = SetupReadonlyScope();
					break;
				case PoolMode.Wait:
					scope = Scopes.Take();
					break;
				default:
					if (!Scopes.TryTake(out scope))
						scope = SetupReadonlyScope();
					break;
			}
			scope.Principal = principal;
			return scope;
		}

		public void Release(Scope scope, bool valid)
		{
			switch (Mode)
			{
				case PoolMode.None:
					Queries.EndQuery(scope.Query, valid);
					scope.Factory.Dispose();
					break;
				default:
					if (valid && !scope.Query.InTransaction && Scopes.Count < Size)
					{
						Scopes.Add(scope);
					}
					else
					{
						Queries.EndQuery(scope.Query, valid);
						scope.Factory.Dispose();
						if (Scopes.Count < Size)
							Scopes.Add(SetupReadonlyScope());
					}
					break;
			}
		}

		public void Dispose()
		{
			try
			{
				foreach (var s in Scopes)
				{
					try
					{
						Queries.EndQuery(s.Query, false);
						s.Factory.Dispose();
					}
					catch (Exception ex)
					{
						TraceSource.TraceEvent(TraceEventType.Error, 5303, "{0}", ex);
					}
				}
				Scopes.Dispose();
			}
			catch (Exception ex2)
			{
				TraceSource.TraceEvent(TraceEventType.Error, 5304, "{0}", ex2);
			}
		}
	}
}
