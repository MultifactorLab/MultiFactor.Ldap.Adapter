namespace MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators
{
    public interface INameTranslator
    {
        string Translate(NameResolverContext context, string from);
    }
}
