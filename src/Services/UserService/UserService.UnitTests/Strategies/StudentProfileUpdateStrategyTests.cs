using System;
using UserService.Application.Strategies;
using UserService.Domain.Entities;
using UserService.Domain.Models;
using Xunit;

namespace UserService.UnitTests.Strategies
{
    public class StudentProfileUpdateStrategyTests
    {
        private readonly StudentProfileUpdateStrategy _strategy;

        public StudentProfileUpdateStrategyTests()
        {
            _strategy = new StudentProfileUpdateStrategy();
        }

        [Fact]
        public void Supports_StudentRole_ReturnsTrue()
        {
            Assert.True(_strategy.Supports("Student"));
            Assert.True(_strategy.Supports("student")); // Case insensitive
        }

        [Fact]
        public void UpdateProfile_MissingAcademicLevel_ReturnsError()
        {
            // Arrange
            var profile = new Profile(Guid.NewGuid());
            var request = new ProfileUpdateRequest
            {
                DisplayName = "Test User",
                AcademicLevel = null // Missing
            };

            // Act
            var result = _strategy.UpdateProfile(profile, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("AcademicLevel is required for students.", result.Errors);
        }

        [Fact]
        public void UpdateProfile_ValidRequest_Success()
        {
            // Arrange
            var profile = new Profile(Guid.NewGuid());
            var request = new ProfileUpdateRequest
            {
                DisplayName = "Test User",
                AcademicLevel = "Undergraduate"
            };

            // Act
            var result = _strategy.UpdateProfile(profile, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Undergraduate", profile.AcademicLevel);
            Assert.Equal("Test User", profile.DisplayName);
        }
    }
}
