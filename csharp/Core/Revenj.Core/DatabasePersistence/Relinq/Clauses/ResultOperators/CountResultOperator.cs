// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
// 
using System;
using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq.Clauses.StreamedData;

namespace Remotion.Linq.Clauses.ResultOperators
{
	/// <summary>
	/// Represents counting the number of items returned by a query.
	/// This is a result operator, operating on the whole result set of a query.
	/// </summary>
	/// <remarks>
	/// "Count" query methods taking a predicate are represented as a combination of a <see cref="WhereClause"/> and a <see cref="CountResultOperator"/>.
	/// </remarks>  /// <example>
	/// In C#, the "Count" call in the following example corresponds to a <see cref="CountResultOperator"/>.
	/// <code>
	/// var query = (from s in Students
	///              select s).Count();
	/// </code>
	/// </example>
	public class CountResultOperator : ValueFromSequenceResultOperatorBase
	{
		public override ResultOperatorBase Clone(CloneContext cloneContext)
		{
			return new CountResultOperator();
		}

		public override StreamedValue ExecuteInMemory<T>(StreamedSequence input)
		{
			var sequence = input.GetTypedSequence<T>();
			var result = sequence.Count();
			return new StreamedValue(result, (StreamedValueInfo)GetOutputDataInfo(input.DataInfo));
		}

		public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo)
		{
			return new StreamedScalarValueInfo(typeof(int));
		}

		/// <inheritdoc />
		public override void TransformExpressions(Func<Expression, Expression> transformation)
		{
			//nothing to do here
		}

		public override string ToString()
		{
			return "Count()";
		}
	}
}
