using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace TheGuide.Systems
{
	/// <summary>
	/// GuideJson class
	/// </summary>
	public abstract class GuideJson
	{
		public virtual string Serialize() =>
			this.ToJson();

		public virtual void Validate()
		{
		}

		public virtual void Validate(long? id)
		{
		}

		public virtual void Validate(ulong? id)
		{

		}

		public override string ToString()
		{
			var fields =
			this.GetType()
			.GetFields()
			.Select(fi => new { FieldName = fi.Name, FieldValue = fi.GetValue(this) })
			.ToDictionary(x => x.FieldName, x => x.FieldValue);

			return string.Join("\n", fields.Select(x => $"**{x.Key.AddSpacesToSentence().Uncapitalize()}**: {x.Value}").ToArray());
		}
	}
}
