using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TheGuide.Systems
{
	public interface IGuideJson
	{
		string Serialize();
		bool Validate();
	}

    public abstract class GuideJson : IGuideJson
    {
		public virtual string Serialize() =>
			JsonConvert.SerializeObject(this);

		public virtual bool Validate() => true;

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
