using System;
using Newtonsoft.Json.Linq;
using PokerBotCore.Entities;
using Qiwi.BillPayments.Client;
using Qiwi.BillPayments.Model;
using Qiwi.BillPayments.Model.In;
using RestSharp;

namespace PokerBotCore.Payments
{
    public static class Transactions
    {
        private static readonly BillPaymentsClient Client = BillPaymentsClientFactory.Create(
            secretKey:
            "eyJ2ZXJzaW9uIjoiUDJQIiwiZGF0YSI6eyJwYXlpbl9tZXJjaGFudF9zaXRlX3VpZCI6InBndDY4Ni0wMCIsInVzZXJfaWQiOiIzODA2NjYzMjA3OTAiLCJzZWNyZXQiOiIzZGE4MDliMTYzMjBkYjJhNGI0NmRhMTg0NjJmYTE5ODcyMjNhZGE1MDExZDI4NDI5ZTM5M2YxOTE1Zjg1MzhmIn19");

        public static string NewTransaction(int sum, User user, ref string billId)
        {
            try
            {
                var response = Client.CreateBill(
                    info: new CreateBillInfo
                    {
                        BillId = Guid.NewGuid().ToString(),
                        Amount = new MoneyAmount
                        {
                            ValueDecimal = sum,
                            CurrencyEnum = CurrencyEnum.Rub
                        },
                        ExpirationDateTime = DateTime.Now.AddDays(5),
                        Customer = new Customer
                        {
                            Account = user.Id.ToString()
                        }
                    });
                billId = response.BillId;
                return response.PayUrl.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static bool CheckPay(User user, string billId)
        {
            try
            {
                using Db db = new Db();
                var response = Client.GetBillInfo(billId);
                if (response.Status.ValueEnum == BillStatusEnum.Paid)
                {
                    user.AddMoney((int) response.Amount.ValueDecimal);
                    user.Referal?.AddMoney((int) (response.Amount.ValueDecimal * (decimal) 0.07));
                    db.SaveChanges();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool OutputTransaction(string number, User user)
        {
            try
            {
                using Db db = new Db();
                int money = (int) (user.output * 0.85);
                var client = new RestClient("https://edge.qiwi.com/sinap/api/v2/terms/99/payments");
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Authorization", "Bearer 2834c05f3b6d08b019d5c6644e98bb4b");
                DateTime date = DateTime.Now;
                uint unixTime = (uint) (date.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                    .TotalSeconds;
                request.AddParameter("application/json",
                    $"{{ \n        \"id\":\"{unixTime * 1000}\", \n        \"sum\": {{ \n          \"amount\":{money}, \n          \"currency\":\"643\" \n        }}, \n        \"paymentMethod\": {{ \n          \"type\":\"Account\", \n          \"accountId\":\"643\" \n        }},\n        \"comment\":\"Выплата с PokerBot {DateTime.Now:dd.MMM.yyyy}\", \n        \"fields\": {{ \n          \"account\":\"{number}\" \n        }} \n      }}",
                    ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                dynamic jobject = JObject.Parse(response.Content);
                if (jobject.transaction.state.code.ToString() != "Accepted") return false;
                db.Transactions.Add(new Transaction
                    {User = user, Money = user.output, Number = number, Date = DateTime.Now.ToString("dd.MMM.yyyy")});
                db.SaveChanges();
                user.Money -= user.output;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}