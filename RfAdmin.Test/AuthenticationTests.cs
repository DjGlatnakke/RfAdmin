using System.Net.Http.Headers;
using System.Net;
using System.Text;
using Moq;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using System.Collections;
using Microsoft.Azure.Functions.Worker;
using System.Security.Claims;
using static RfAdmin.Test.AuthenticationTests;

namespace RfAdmin.Test
{
    [TestClass]
    public class AuthenticationTests
    {
        private static Authentication auth;
        private static string createdRfId = "1234";
        private static string notAuthRfId = "12345";
        private static string[] validRfIds = new[]{ createdRfId };
        [ClassInitialize]
        public static void InitTests(TestContext context)
        {
            //Setup
            var loggerMock = new Mock<ILogger<Authentication>>();

            var authTrue = new Mock<Azure.NullableResponse<RfTag>>();
            authTrue.SetupGet(x => x.HasValue).Returns(true);

            var authFalse = new Mock<Azure.NullableResponse<RfTag>>();
            authFalse.SetupGet(x => x.HasValue).Returns(false);

            var createSuccess = new Mock<Azure.Response>();
            createSuccess.SetupGet(x => x.Status).Returns((int)HttpStatusCode.Created);

            var createAlreadyExists = new Mock<Azure.Response>();
            createAlreadyExists.SetupGet(x => x.Status).Returns((int)HttpStatusCode.Conflict);

            var tableClientMock = new Mock<TableClient>();
            tableClientMock
                .Setup(_ => _.GetEntityIfExistsAsync<RfTag>("Tags", createdRfId, default, default))
                .Returns(Task.FromResult(authTrue.Object));
            tableClientMock
                .Setup(_ => _.GetEntityIfExistsAsync<RfTag>(It.IsAny<string>(), It.IsNotIn(validRfIds), default, default))
                .Returns(Task.FromResult(authFalse.Object));

            tableClientMock
                .Setup(_ => _.AddEntityAsync<RfTag>(It.Is<RfTag>(rf => rf.RowKey == createdRfId), default))
                .Returns(Task.FromResult(createAlreadyExists.Object));

            tableClientMock
                .Setup(_ => _.AddEntityAsync<RfTag>(It.Is<RfTag>(rf => rf.RowKey != createdRfId), default))
                .Returns(Task.FromResult(createSuccess.Object));


            var tableServiceClientMock = new Mock<TableServiceClient>();
            tableServiceClientMock.Setup(_ => _.GetTableClient(It.IsAny<string>()))
                .Returns(tableClientMock.Object);

            auth = new Authentication(loggerMock.Object, tableServiceClientMock.Object);
        }

        [TestMethod]
        public async Task Authenticate_Get()
        {
            Mock<HttpRequestData> requestMock = GetRequestData("GET");

            var authenticated = await auth.Authenticate(requestMock.Object, createdRfId);
            Assert.IsTrue(authenticated, $"RfId {createdRfId} was not authenticated but should have been");

            var notAuthenticated = await auth.Authenticate(requestMock.Object, notAuthRfId);
            Assert.IsFalse(notAuthenticated, $"Rfid {notAuthRfId} was authenticated but should not have been");

        }

        [TestMethod]
        public async Task Create_Post()
        {
            Mock<HttpRequestData> conflictRequestMock = GetRequestData("POST", "1234");
            Mock<HttpRequestData> successRequestMock = GetRequestData("POST", "12345");

            var created = await auth.Create(successRequestMock.Object);
            Assert.AreEqual((int)HttpStatusCode.Created, created.StatusCode, $"Creation gave statuscode {created.StatusCode} but expected {(int)HttpStatusCode.Created}");

            var conflict = await auth.Create(conflictRequestMock.Object);
            Assert.AreEqual((int)HttpStatusCode.Conflict, conflict.StatusCode, $"Creation gave statuscode {conflict.StatusCode} but expected {(int)HttpStatusCode.Conflict}");
        }

        private static Mock<HttpRequestData> GetRequestData(string method, string rfIdBody = null)
        {
            var functionContext = new Mock<FunctionContext>();

            var body = JsonSerializer.Serialize(new { rfid = rfIdBody });
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            var requestMock = new Mock<HttpRequestData>(functionContext.Object);
            if (rfIdBody != null)
                requestMock.SetupGet(r => r.Body).Returns(stream);

            requestMock.Setup(_ => _.CreateResponse(It.IsAny<HttpStatusCode>()))
                .Returns((HttpStatusCode code) => { return new MockHttpResponseData(functionContext.Object, code); });

            return requestMock;
        }

        public class MockHttpResponseData : HttpResponseData
        {
            public MockHttpResponseData(FunctionContext functionContext, HttpStatusCode status)
                : base(functionContext)
            {
                StatusCode = status;
            }

            public override HttpStatusCode StatusCode { get; set; }
            public override HttpHeadersCollection Headers { get; set; } = new HttpHeadersCollection();
            public override Stream Body { get; set; } = new MemoryStream();
            public override HttpCookies Cookies { get; }
        }
    }
}
