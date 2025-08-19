
using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using IdeaProjectExamPrep.Models;


namespace IdeaProjectExamPrep
{ 

[TestFixture]
    public class IdeaProjectApiTests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;
        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJmZjkyMmExYS00ZTdiLTQ4MDAtODQzOC0yYzY3N2UwN2IxZDgiLCJpYXQiOiIwOC8xOC8yMDI1IDE0OjA0OjE4IiwiVXNlcklkIjoiMjE0OTcwODQtZGEzMy00YzQ3LWQzMGMtMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJldmthZXZrYUBldmthLmJnIiwiVXNlck5hbWUiOiJldmthZXhhbSIsImV4cCI6MTc1NTU0NzQ1OCwiaXNzIjoiSWRlYUNlbnRlcl9BcHBfU29mdFVuaSIsImF1ZCI6IklkZWFDZW50ZXJfV2ViQVBJX1NvZnRVbmkifQ.u0zruMSpMGNzvWcScGyfq9OsceFiNL8rl5ZgQU3oUv4";
        private const string LoginEmail = "evkaevka@evka.bg";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;
            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);

            }
            var options = new RestClientOptions(BaseUrl)

            { Authenticator = new JwtAuthenticator(jwtToken) };
            this.client = new RestClient(options);

        }

        private string GetJwtToken(string email, string password)
        { var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new
            { email, password });

            var response = tempClient.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }
        [Order(1)]
        [Test]
        public void CreateAnewIdeaWithTheRequiredFields()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "This is a test idea",
                Url = ""
            };
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(createResponse.Msg, Is.Not.Null,"Response should not be null.");
             
        }
        [Order(2)]
        [Test]
        public void GetAllIdeas()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);
            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseItems, Is.Not.Null);
            Assert.That(responseItems,Is.Not.Empty);
            lastCreatedIdeaId = responseItems.LastOrDefault()?.Id;

        }
        [Order(3)]
        [Test]
        public void EditTheLastCreatedIdea()
        { 
            var editRequest = new IdeaDTO
            {
                Title = "Updated Test Idea",
                Description = "This is an updated test idea.",
                Url = null
            };
            
            var request = new RestRequest($"/api/Idea/Edit",Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));
         


        }

        [Order(4)]
        [Test]
        public void DeleteTheEditedIdea()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
         
        }
        public void CreateIdea_WithoutRequiredFields_ShouldReturnSuccessAgain()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }

        [Order(6)]
        [Test]

        public void EditNonExistingIdea_ShouldReturnNotFound()
        {
            string nonExistingIdeaId = "123";
            var editRequest = new IdeaDTO
            {
                Title = "Edited Non-Existing Idea",
                Description = "This is an updated test idea description for a non-existing idea.",
                Url = ""
            };
            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", nonExistingIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [Order(7)]
        [Test]

        public void DeleteNonExistingIdea_ShouldReturnNotFound()
        {
            string nonExistingIdeaId = "123";
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", nonExistingIdeaId);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {


            this.client?.Dispose();
        }
    

        }

    }

    