
namespace MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators
{
    public class ProfileToSamAccountNameTranslator : INameTranslator
    {
        public string Translate(NameResolverContext nameResolverContext, string from)
        {
            var uid = nameResolverContext.Profile?.Uid;
            if (!string.IsNullOrEmpty(uid))
            {
                return uid;
            }
            return from;
        }
    }
}
