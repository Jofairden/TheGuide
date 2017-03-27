using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheGuide.Systems.TEMP
{
	//public sealed class ValidationObjectConverter : JsonConverter
	//{
	//	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	//	{
	//		if (value == null)
	//		{
	//			serializer.Serialize(writer, null);
	//			return;
	//		}

	//		var baseJson =
	//	}

	//	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	//	{
	//		// gets T
	//		//var genericType = objectType.GetTypeInfo().GetGenericArguments()[0];
	//		var instance = (dynamic) Activator.CreateInstance(objectType);
	//		instance.Default = 
	//		return instance;
	//	}

	//	public override bool CanConvert(Type objectType)
	//	{
	//		return objectType == typeof(ValidationObject<>);
	//	}
	//}

	//[JsonConverter(typeof(ValidationObjectConverter))]
	public sealed class ValidationObject<T> : IValidatable where T : IComparable<T>, IConvertible
	{
		public T Default { get; set; } = default(T);
		private T _content;
		public T Content
		{
			get { return _content; }
			set { _content = value ; }
		}

		public ValidationObject(T t = default(T))
		{
			Content = t;
		}

		public bool Validate() => Content.GetHashCode() != default(T)?.GetHashCode();

		public override string ToString()
		{
			return _content.ToString(CultureInfo.InvariantCulture);
		}

		public static implicit operator ValidationObject<T>(T t) => new ValidationObject<T>(t);
		public static implicit operator T(ValidationObject<T> t) => t.Content;

		public C ToObject<C>() where C : IConvertible
		{
			try
			{
				return (C)Convert.ChangeType(this, typeof(C));
			}
			catch (InvalidCastException)
			{
				return default(C);
			}
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ValidationObject<T>))
				return false;

			return this.Equals((ValidationObject<T>)obj);
		}

		public bool Equals(ValidationObject<T> other)
		{
			return EqualityComparer<T>.Default.Equals(_content, other._content);
		}

		public override int GetHashCode()
		{
			return EqualityComparer<T>.Default.GetHashCode(_content);
		}

		public ValidationObject<T> Clone()
		{
			var clone = (ValidationObject<T>)this.MemberwiseClone();
			clone.Default = Default;
			clone.Content = _content;
			return clone;
		}

		public void SetDefault()
		{
			Content = typeof(T) == typeof(DateTime) ? (T)(object)DateTime.Now : Default;
		}
	}
}
