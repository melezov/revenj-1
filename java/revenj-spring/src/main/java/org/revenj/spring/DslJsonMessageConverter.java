package org.revenj.spring;

import org.revenj.serialization.json.DslJsonSerialization;
import org.springframework.http.HttpInputMessage;
import org.springframework.http.HttpOutputMessage;
import org.springframework.http.MediaType;
import org.springframework.http.converter.AbstractGenericHttpMessageConverter;
import org.springframework.http.converter.GenericHttpMessageConverter;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.http.converter.HttpMessageNotWritableException;

import java.io.IOException;
import java.lang.reflect.Type;

public class DslJsonMessageConverter extends AbstractGenericHttpMessageConverter<Object> implements GenericHttpMessageConverter<Object> {
	private final DslJsonSerialization serializer;

	public DslJsonMessageConverter(DslJsonSerialization serializer) {
		super(MediaType.APPLICATION_JSON);
		this.serializer = serializer;
	}

	public boolean canRead(Class<?> clazz, MediaType mediaType) {
		return serializer.canDeserialize(clazz) && canRead(mediaType);
	}

	public boolean canRead(Type type, Class<?> contextClass, MediaType mediaType) {
		return serializer.canDeserialize(type) && canRead(mediaType);
	}

	public boolean canWrite(Type type, Class<?> clazz, MediaType mediaType) {
		return serializer.canSerialize(type) && canWrite(mediaType);
	}

	public boolean canWrite(Class<?> clazz, MediaType mediaType) {
		return serializer.canSerialize(clazz) && canWrite(mediaType);
	}

	protected boolean supports(Class<?> clazz) {
		return serializer.canSerialize(clazz);
	}

	protected Object readInternal(Class<?> clazz, HttpInputMessage inputMessage) throws IOException, HttpMessageNotReadableException {
		return serializer.deserialize(clazz, inputMessage.getBody());
	}

	public Object read(Type type, Class<?> contextClass, HttpInputMessage inputMessage) throws IOException, HttpMessageNotReadableException {
		return serializer.deserialize(type, inputMessage.getBody());
	}

	protected void writeInternal(Object instance, Type type, HttpOutputMessage outputMessage) throws IOException, HttpMessageNotWritableException {
		serializer.serialize(instance, outputMessage.getBody());
	}

	protected MediaType getDefaultContentType(Object object) throws IOException {
		return MediaType.APPLICATION_JSON;
	}
}
