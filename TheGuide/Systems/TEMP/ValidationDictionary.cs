using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TheGuide.Systems.TEMP
{
	public sealed class ValidationDictionary<K, V> : IValidatable, IDictionary<K, V> where V : IComparable<V>, IConvertible where K : IComparable<K>, IConvertible
	{
		public Tuple<K, V> Default { get; set; } = new Tuple<K, V>(default(K), default(V));
		private IDictionary<ValidationObject<K>, ValidationObject<V>> _content {get;set; }

		public IReadOnlyDictionary<K, V> Content =>
			(IReadOnlyDictionary<K, V>)_content.ToDictionary(x => x.Key, x => x.Value);

		public ValidationDictionary(IDictionary<K, V> source = null)
		{
			_content =
				source.ToDictionary(x => new ValidationObject<K>(x.Key), y => new ValidationObject<V>(y.Value))
				?? new Dictionary<ValidationObject<K>, ValidationObject<V>>();
		}

		public static implicit operator ValidationDictionary<K, V>(Dictionary<K, V> d) =>
			new ValidationDictionary<K, V>(d);
		public static implicit operator Dictionary<K, V>(ValidationDictionary<K, V> d) => 
			d._content.ToDictionary(x => x.Key.Content, x => x.Value.Content);

		public KeyValuePair<ValidationObject<K>, ValidationObject<V>> Make(K k, V v) =>
			new KeyValuePair<ValidationObject<K>, ValidationObject<V>>(new ValidationObject<K>(k) {Default = Default.Item1}, new ValidationObject<V>(v) {Default = Default.Item2});

		public KeyValuePair<ValidationObject<K>, ValidationObject<V>> Make(KeyValuePair<K, V> kvp) =>
			Make(kvp.Key, kvp.Value);

		public bool Validate() =>
			_content.All(x =>
				x.Value.Validate()
				&& x.Key.Validate());

		public override bool Equals(object obj)
		{
			if (!(obj is ValidationDictionary<K, V>))
				return false;

			return this.Equals((ValidationDictionary<K, V>)obj);
		}

		private bool Equals(ValidationDictionary<K, V> other)
		{
			return Equals(_content, other._content);
		}

		public override int GetHashCode()
		{
			return _content?.GetHashCode() ?? 0;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
		{
			return _content.Select(x => new KeyValuePair<K, V>(x.Key.Content, x.Value.Content)).GetEnumerator();
		}

		public void Add(KeyValuePair<K, V> item)
		{
			_content.Add(Make(item.Key, item.Value));
		}

		public void Clear()
		{
			_content.Clear();
		}

		public bool Contains(KeyValuePair<K, V> item)
		{
			return _content.Contains(Make(item));
		}

		public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
		{
		}

		public bool Remove(KeyValuePair<K, V> item)
		{
			return _content.Remove(Make(item));
		}

		public int Count => _content.Count;

		public bool IsReadOnly => _content.IsReadOnly;

		public void Add(K key, V value)
		{
			_content.Add(Make(key, value));
		}

		public bool ContainsKey(K key)
		{
			return _content.ContainsKey(new ValidationObject<K>(key));
		}

		public bool Remove(K key)
		{
			if (!ContainsKey(key))
				return false;
			_content[new ValidationObject<K>(key)] = new ValidationObject<V>();
			return true;
		}

		public bool TryGetValue(K key, out V value)
		{
			var cts = ContainsKey(key);
			value =
				cts
				? _content[new ValidationObject<K>(key)].Content
				: default(V);
			return cts;
		}

		public V this[K key]
		{
			get { return _content[new ValidationObject<K>(key)].Content; }
			set { _content[new ValidationObject<K>(key)] = new ValidationObject<V>(value); }
		}

		public ICollection<K> Keys
		{
			get { return _content.Select(x => x.Key) as ICollection<K>; }
		}

		public ICollection<V> Values
		{
			get { return _content.Select(x => x.Value) as ICollection<V>; }
		}

		public ValidationDictionary<K, V> Clone()
		{
			return new ValidationDictionary<K, V>((IDictionary<K, V>)Content);
		}

		public void SetDefault()
		{
			_content.ToList().ForEach(x =>
			{
				x.Key.Default = Default.Item1;
				x.Value.Default = Default.Item2;
			});
		}
	}
}
