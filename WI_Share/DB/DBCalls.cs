using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WI_Share.DB
{
    public class DBCalls
    {
        const string host = "http://51.15.236.147:8086";
        const string token = "2N1X7WD4MEIM6VLVoNs_HQ6zSbrQJK4pDGC9Hy40dXLHexv_eqrvIauqKZ-t4FSpoV-mZaAiL24mrPAeSi0Mmw==";
        const string bucket = "iot";
        const string organization = "kiro";
        private readonly InfluxDBClient client;

        public DBCalls()
        {

            var options = new InfluxDBClientOptions.Builder()
                .Url(host)
                .AuthenticateToken(token.ToCharArray())
                .Org(organization)
                .Bucket(bucket)
                .Build();

            client = InfluxDBClientFactory.Create(options)
               .EnableGzip();
        }

        public async Task AddData(DomainEntity entity)
        {
            var converter = new DomainEntityConverter();
            await client.GetWriteApiAsync(converter)
                .WriteMeasurementsAsync(WritePrecision.S, entity);
        }

        public IEnumerable<DomainEntity> GetLatest()
        {
            var converter = new DomainEntityConverter();
            var queryApi = client.GetQueryApiSync(converter);
            var list = InfluxDBQueryable<DomainEntity>
                .Queryable(bucket, organization, queryApi, converter)
                .Where(x => x.Timestamp >= DateTime.Now.AddSeconds(-6))
                .ToList();

            return list;
        }
    }
}
