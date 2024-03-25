//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

namespace MultiFactor.Ldap.Adapter.Services.MultiFactorApi
{
    public class MultiFactorApiResponse<TModel>
    {
        public bool Success { get; set; }

        public TModel Model { get; set; }
    }
}
