using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace TheGuide.Systems
{
    public class GuideResult : IResult
    {
		public GuideResult(string y = null, bool z = false)
		{
			Error = null;
			ErrorReason = y;
			IsSuccess = z;
		}

		public void SetCommandError(CommandError? x)
		{
			Error = x;
		}

		public void SetErrorReason(string x)
		{
			ErrorReason = x;
		}

		public void SetIsSuccess(bool x)
		{
			IsSuccess = x;
		}

		public CommandError? Error { get; private set; } = null;

		public string ErrorReason { get; private set; } = null;

		public bool IsSuccess { get; private set; } = false;
	}
}
