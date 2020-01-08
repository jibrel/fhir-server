﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Tests.Common;
using Microsoft.Health.Fhir.Tests.Common.FixtureParameters;
using Microsoft.Health.Fhir.Tests.E2E.Common;
using Xunit;
using static Hl7.Fhir.Model.OperationOutcome;
using FhirClient = Microsoft.Health.Fhir.Tests.E2E.Common.FhirClient;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Tests.E2E.Rest
{
    [HttpIntegrationFixtureArgumentSets(DataStore.All, Format.All)]
    public class ObservationResolveReferenceTests : IClassFixture<HttpIntegrationTestFixture>
    {
        public ObservationResolveReferenceTests(HttpIntegrationTestFixture fixture)
        {
            Client = fixture.FhirClient;
        }

        protected FhirClient Client { get; set; }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task GivenAResourceWithSearchUrlReference_WhenPostResourceAndIdentifierExists_ThenServerShouldReturnSuccess()
        {
            var observation = Samples.GetDefaultObservation().ToPoco<Observation>();
            var patient = Samples.GetDefaultPatient().ToPoco<Patient>();
            var uniqueIdentifier = Guid.NewGuid().ToString();

            patient.Identifier.First().Value = uniqueIdentifier;

            // Create patient resource
            await Client.CreateAsync(patient);

            observation.Subject.Reference = $"Patient?identifier={uniqueIdentifier}";

            FhirResponse<Observation> updateResponse = await Client.CreateAsync(
                observation);

            Assert.Equal(HttpStatusCode.Created, updateResponse.StatusCode);

            Observation updatedResource = updateResponse.Resource;

            Assert.NotNull(updatedResource);
            Assert.NotNull(updatedResource.Id);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task GivenAResourceWithSearchUrlReference_WhenPostResourceAndIdenfierNotExists_TheServerShouldReturnBadRequest()
        {
            var observation = Samples.GetDefaultObservation().ToPoco<Observation>();
            var uniqueIdentifier = Guid.NewGuid().ToString();

            observation.Subject.Reference = $"Patient?identifier={uniqueIdentifier}";

            var exception = await Assert.ThrowsAsync<FhirException>(() => Client.CreateAsync(observation));
            var issue = exception.Response.Resource.Issue.First();
            Assert.Equal(HttpStatusCode.BadRequest, exception.Response.StatusCode);
            Assert.Equal(IssueSeverity.Error, issue.Severity.Value);
            Assert.Equal(IssueType.Invalid, issue.Code);
            Assert.Equal($"Given conditional reference 'Patient?identifier={uniqueIdentifier}' does not resolve to a resource.", issue.Diagnostics);
        }
    }
}
