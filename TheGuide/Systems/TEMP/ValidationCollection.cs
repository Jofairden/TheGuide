using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace TheGuide.Systems.TEMP
{
	public sealed class ValidationCollection<T> : IValidatable, ICollection<T> where T : IComparable<T>, IConvertible
	{
		public T Default { get; set; }
		private ICollection<ValidationObject<T>> _content {get;set; }

		public IEnumerable<T> Content => AsReadOnly().Select(x => x.Content);

		private IEnumerable<ValidationObject<T>> AsReadOnly()
		{
			foreach (var o in _content)
			{
				yield return o.Clone();
			}
		}

		public ValidationCollection(ICollection<T> source = null)
		{
			_content = source.Select(x => new ValidationObject<T>(x)).ToList() ?? new List<ValidationObject<T>>();
		}

		public static implicit operator ValidationCollection<T>(List<T> t)
			=> new ValidationCollection<T>(t.Select(x => new ValidationObject<T>(x)) as ICollection<T>);
		public static implicit operator List<T>(ValidationCollection<T> t)
			=> t._content.Select(x => x.Content).ToList();

		public void ForEach(Action<ValidationObject<T>> a)
		{
			_content.ToList().ForEach(a);
		}

		public void Add(T obj)
		{
			_content.Add(new ValidationObject<T>(obj) {Default = this.Default});
		}

		public void Remove(T obj)
		{
			_content.Remove(new ValidationObject<T>(obj));
		}

		public T ElementAt(int index)
		{
			return _content.ElementAt(index).Content;
		}

		public bool Validate() => _content.All(x => x.Validate());

		public override bool Equals(object obj)
		{
			if (!(obj is ValidationCollection<T>))
				return false;

			return this.Equals((ValidationCollection<T>)obj);
		}

		private bool Equals(ValidationCollection<T> other)
		{
			return Equals(_content, other._content);
		}

		public override int GetHashCode()
		{
			return _content?.GetHashCode() ?? 0;
		}

		public ValidationCollection<T> Clone()
		{
			return new ValidationCollection<T>((ICollection<T>)_content);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _content.Select(x => x.Content).GetEnumerator();
		}

		public void Clear()
		{
			_content.Clear();
		}

		public bool Contains(T item)
		{
			return _content.Contains(new ValidationObject<T>(item));
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
		}

		bool ICollection<T>.Remove(T item)
		{
			return _content.Remove(new ValidationObject<T>(item)); ;
		}

		public int Count => _content.Count;

		public bool IsReadOnly => _content.IsReadOnly;

		public void SetDefault()
		{
			_content.ToList().ForEach(x =>
			{
				x.Default = Default;
			});
		}
	}
}
