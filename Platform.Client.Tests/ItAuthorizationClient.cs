using System.Collections.Generic;
using InfluxData.Platform.Client.Client;
using InfluxData.Platform.Client.Domain;
using NUnit.Framework;
using Task = System.Threading.Tasks.Task;

namespace Platform.Client.Tests
{
    [TestFixture]
    public class ItAuthorizationClient : AbstractItClientTest
    {
        private AuthorizationClient _authorizationClient;
        private User _user;

        [SetUp]
        public new async Task SetUp()
        {
            _authorizationClient = PlatformClient.CreateAuthorizationClient();
            _user = await PlatformClient.CreateUserClient().CreateUser(GenerateName("Auth User"));
        }

        [Test]
        public async Task CreateAuthorization()
        {
            var readUsers = new Permission
            {
                Action = Permission.ReadAction,
                Resource = Permission.UserResource
            };

            var writeOrganizations = new Permission
            {
                Action = Permission.WriteAction,
                Resource = Permission.OrganizationResource
            };

            var permissions = new List<Permission> {readUsers, writeOrganizations};

            var authorization = await _authorizationClient.CreateAuthorization(_user, permissions);

            Assert.IsNotNull(authorization);
            Assert.IsNotEmpty(authorization.Id);
            Assert.IsNotEmpty(authorization.Token);
            Assert.AreEqual(authorization.UserId, _user.Id);
            Assert.AreEqual(authorization.UserName, _user.Name);
            Assert.AreEqual(authorization.Status, Status.Active);

            Assert.AreEqual(authorization.Permissions.Count, 2);
            Assert.AreEqual(authorization.Permissions[0].Resource, "user");
            Assert.AreEqual(authorization.Permissions[0].Action, "read");
            Assert.AreEqual(authorization.Permissions[1].Resource, "org");
            Assert.AreEqual(authorization.Permissions[1].Action, "write");

            var links = authorization.Links;

            Assert.That(links.Count == 2);
            Assert.AreEqual($"/api/v2/authorizations/{authorization.Id}", links["self"]);
            Assert.AreEqual($"/api/v2/users/{_user.Id}", links["user"]);
        }
        
        [Test]
        //TODO
        [Ignore("updateAuthorization return PlatformError but c.db.update() required 'plain' go error bolt/authorization.go:397")]
        public async Task UpdateAuthorizationStatus() {

            var readUsers = new Permission
            {
                Action = Permission.ReadAction,
                Resource = Permission.UserResource
            };

            var permissions = new List<Permission> {readUsers};

            var authorization = await _authorizationClient.CreateAuthorization(_user, permissions);

            Assert.AreEqual(authorization.Status, Status.Active);

            authorization.Status = Status.Inactive;
            authorization = await _authorizationClient.UpdateAuthorization(authorization);

            Assert.AreEqual(authorization.Status, Status.Inactive);

            authorization.Status = Status.Active;
            authorization = await _authorizationClient.UpdateAuthorization(authorization);

            Assert.AreEqual(authorization.Status, Status.Active);
        }
        
        [Test]
        public async Task FindAuthorizations() {

            var size = (await _authorizationClient.FindAuthorizations()).Count;

            await _authorizationClient.CreateAuthorization(_user, new List<Permission>());

            var authorizations = await _authorizationClient.FindAuthorizations();
            
            Assert.AreEqual(size + 1, authorizations.Count);
        }
        
        [Test]
        public async Task FindAuthorizationsById() {

            var authorization = await _authorizationClient.CreateAuthorization(_user, new List<Permission>());

            var foundAuthorization = await _authorizationClient.FindAuthorizationById(authorization.Id);

            Assert.IsNotNull(foundAuthorization);
            Assert.AreEqual(authorization.Id, foundAuthorization.Id);
            Assert.AreEqual(authorization.Token, foundAuthorization.Token);
            Assert.AreEqual(authorization.UserId, foundAuthorization.UserId);
            Assert.AreEqual(authorization.UserName, foundAuthorization.UserName);
            Assert.AreEqual(authorization.Status, foundAuthorization.Status);
        }

        [Test]
        public async Task FindAuthorizationsByIdNull() {

            var authorization = await _authorizationClient.FindAuthorizationById("020f755c3c082000");

            Assert.IsNull(authorization);
        }
        
        [Test]
        public async Task DeleteAuthorization() {

            var createdAuthorization = await _authorizationClient.CreateAuthorization(_user, new List<Permission>());
            Assert.IsNotNull(createdAuthorization);

            var foundAuthorization = await _authorizationClient.FindAuthorizationById(createdAuthorization.Id);
            Assert.IsNotNull(foundAuthorization);

            // delete authorization
            await _authorizationClient.DeleteAuthorization(createdAuthorization);

            foundAuthorization = await _authorizationClient.FindAuthorizationById(createdAuthorization.Id);
            Assert.IsNull(foundAuthorization);
        }
        
        [Test]
        public async Task  FindAuthorizationsByUser()
        {
            var size = (await _authorizationClient.FindAuthorizationsByUser(_user)).Count;

            await _authorizationClient.CreateAuthorization(_user, new List<Permission>());

            var authorizations = await _authorizationClient.FindAuthorizationsByUser(_user);
            Assert.AreEqual(size + 1, authorizations.Count);
        }

        [Test]
        public async Task  FindAuthorizationsByUserName() {

            var size = (await _authorizationClient.FindAuthorizationsByUser(_user)).Count;

            await _authorizationClient.CreateAuthorization(_user, new List<Permission>());

            var authorizations = await _authorizationClient.FindAuthorizationsByUserName(_user.Name);
            Assert.AreEqual(size + 1, authorizations.Count);
        }
    }
}