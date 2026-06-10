namespace EnglishTestWeb.Api.Infrastructure.Authorization.Policies;

public static class AuthorizationPolicies
{
    public const string CanViewClassAsTeacher = "CanViewClassAsTeacher";
    public const string CanViewClassAsStudent = "CanViewClassAsStudent";
    public const string CanViewTemplateAsTeacher = "CanViewTemplateAsTeacher";
}
