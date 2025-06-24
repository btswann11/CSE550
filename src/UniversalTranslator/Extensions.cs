using System;

namespace UniversalTranslator.Extensions;

public static class Extensions
{
    public static bool IsValid(this User user)
        => user is not null
            && !string.IsNullOrWhiteSpace(user.GroupName)
            && !string.IsNullOrWhiteSpace(user.SourceUserId)
            && !string.IsNullOrWhiteSpace(user.TargetUserId)
            && !string.IsNullOrWhiteSpace(user.Message);
}
