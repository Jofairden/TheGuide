using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheGuide.Systems
{
	public interface IGuideJson
	{
		string Serialize();
		void Validate(long? id);
	}

    public abstract class GuideJson : IGuideJson
    {
		public virtual string Serialize() =>
			JsonConvert.SerializeObject(this);

	    public virtual void Validate()
	    {
		    
	    }
	    public virtual void Validate(long? id)
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

		public string ToJson() =>
			JsonConvert.SerializeObject(this);
	}
}
