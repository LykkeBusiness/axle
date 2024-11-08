using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Contracts.Responses;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.ErrorCodes;
using MarginTrading.AccountsManagement.Contracts.Models;
using Refit;

namespace Axle.Services
{
    public class EmptyAccountsApiStub : IAccountsApi
    {
        public Task<List<AccountSuggestedContract>> SuggestedList(string query, int limit)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<AccountContract>> List(string search = null, bool showDeleted = false)
        {
            throw new System.NotImplementedException();
        }

        public Task<PaginatedResponse<AccountContract>> ListByPages(string search = null, int? skip = null, int? take = null, bool showDeleted = false,
            bool isAscendingOrder = true)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<AccountContract>> GetByClient(string clientId, bool showDeleted = false)
        {
            return Task.FromResult(new List<AccountContract>());
        }

        public Task<AccountContract> GetByClientAndId(string clientId, string accountId)
        {
            throw new System.NotImplementedException();
        }

        public Task<AccountContract> GetById(string accountId)
        {
            throw new System.NotImplementedException();
        }

        public Task<ApiResponse<AccountContract>> Create(string clientId, CreateAccountRequestObsolete request)
        {
            throw new System.NotImplementedException();
        }

        public Task<ApiResponse<AccountContract>> Create(CreateAccountRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<AccountContract> Change(string clientId, string accountId, ChangeAccountRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<AccountContract> Change(string accountId, ChangeAccountRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> BeginChargeManually(string accountId, AccountChargeManuallyRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> BeginDeposit(string accountId, AccountChargeRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> BeginWithdraw(string accountId, AccountChargeRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<WithdrawalResponse> TryBeginWithdraw(string accountId, AccountChargeRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<AccountContract>> CreateDefaultAccounts(CreateDefaultAccountsRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<AccountContract>> CreateAccountsForNewBaseAsset(CreateAccountsForBaseAssetRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task Reset(string accountId)
        {
            throw new System.NotImplementedException();
        }

        public Task<AccountStatContract> GetStat(string accountId)
        {
            throw new System.NotImplementedException();
        }

        public Task<decimal?> GetDisposableCapital(string accountId, GetDisposableCapitalRequest request = null)
        {
            throw new System.NotImplementedException();
        }

        public Task RecalculateStat(string accountId)
        {
            throw new System.NotImplementedException();
        }

        public Task<ClientTradingConditionsContract> GetClientTradingConditions(string clientId)
        {
            throw new System.NotImplementedException();
        }

        public Task<PaginatedResponse<ClientTradingConditionsContract>> ListClientsTradingConditions(string tradingConditionId, int skip = 0, int take = 20)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<ClientTradingConditionsContract>> GetAllClientTradingConditions()
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<ClientTradingConditionsWithAccountsContract>> GetAllClientTradingConditionsWithAccounts()
        {
            throw new System.NotImplementedException();
        }

        public Task<ErrorCodeResponse<TradingConditionErrorCodesContract>> UpdateClientTradingConditions(UpdateClientTradingConditionRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<ErrorCodeResponse<TradingConditionErrorCodesContract>> UpdateClientTradingConditions(UpdateClientTradingConditionsBulkRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}