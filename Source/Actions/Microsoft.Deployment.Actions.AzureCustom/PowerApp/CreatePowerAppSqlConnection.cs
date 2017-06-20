﻿using System;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.Helpers;
using Microsoft.Deployment.Common.Model;

namespace Microsoft.Deployment.Actions.AzureCustom.PowerApp
{
    [Export(typeof(IAction))]
    public class CreatePowerAppSqlConnection : BaseAction
    {
        private const int SQL_CONNECTION_ID_LENGTH = 32;

        private const string BASE_POWER_APPS_URL = "https://management.azure.com/providers/Microsoft.PowerApps";

        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            var azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            AzureHttpClient client = new AzureHttpClient(azureToken);

            string newSqlConnectionId = RandomGenerator.GetRandomHexadecimal(SQL_CONNECTION_ID_LENGTH);
            string powerAppEnvironment = request.DataStore.GetValue("PowerAppEnvironment");

            if (powerAppEnvironment == null)
            {
                return new ActionResponse(ActionStatus.Success);
            }

            string sqlConnectionString = request.DataStore.GetValueAtIndex("SqlConnectionString", "SqlServerIndex");
            SqlCredentials sqlCredentials = SqlUtility.GetSqlCredentialsFromConnectionString(sqlConnectionString);

            string body = $"{{\"properties\":{{\"environment\":{{\"id\":\"/providers/Microsoft.PowerApps/environments/{powerAppEnvironment}\",\"name\":\"{powerAppEnvironment}\"}},\"connectionParameters\":{{\"server\":\"{sqlCredentials.Server}\",\"database\":\"{sqlCredentials.Database}\",\"username\":\"{sqlCredentials.Username}\",\"password\":\"{sqlCredentials.Password}\"}}}}}}";
            string url = $"{BASE_POWER_APPS_URL}/apis/shared_sql/connections/{newSqlConnectionId}?api-version=2016-11-01&$filter=environment%20eq%20%27{powerAppEnvironment}%27";

            await client.ExecuteGenericRequestWithHeaderAsync(HttpMethod.Put, url, body);
            //TODO: if create fails return failure

            request.DataStore.AddToDataStore("PowerAppSqlConnectionId", newSqlConnectionId, DataStoreType.Private);

            return new ActionResponse(ActionStatus.Success);
        }
    }
}