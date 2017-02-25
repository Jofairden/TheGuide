using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace TheGuide.Systems
{
	public class TagJson
	{
		public TagJson(TagJson source = null)
		{
			if (source != null)
			{
				Name = source.Name;
				Output = source.Output;
				TimeCreated = source.TimeCreated;
				LastEdited = source.LastEdited;
				Creator = source.Creator;
				LastEditor = source.LastEditor;
			}
		}

		public string Name;
		public string Output;
		public DateTime TimeCreated;
		public DateTime LastEdited;
		public string Creator;
		public string LastEditor;

		public bool Validate()
		{
			return
				!string.IsNullOrEmpty(Name)
				&& !string.IsNullOrEmpty(Output)
				&& !string.IsNullOrEmpty(Creator)
				&& !string.IsNullOrEmpty(LastEditor)
				&& TimeCreated != DateTime.MinValue
				&& LastEdited != DateTime.MinValue;
		}

		public string Serialize() => JsonConvert.SerializeObject(this);

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
