using System;
using UserService.Application.Strategies;
using UserService.Domain.Entities;
using UserService.Domain.Models;
using Xunit;

namespace UserService.UnitTests.Strategies
{
    public class TeacherProfileUpdateStrategyTests
    {
        private readonly TeacherProfileUpdateStrategy _strategy;

        public TeacherProfileUpdateStrategyTests()
        {
            _strategy = new TeacherProfileUpdateStrategy();
        }

        [Fact]
        public void Supports_TeacherRole_ReturnsTrue()
        {
            Assert.True(_strategy.Supports("Teacher"));
        }

        [Fact]
        public void UpdateProfile_MissingInstructorType_ReturnsError()
        {
            // Arrange
            var profile = new Profile(Guid.NewGuid());
            var request = new ProfileUpdateRequest
            {
                DisplayName = "Test Teacher",
                InstructorType = null // Missing
            };

            // Act
            var result = _strategy.UpdateProfile(profile, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("InstructorType is required for teachers.", result.Errors);
        }

        [Fact]
        public void UpdateProfile_ValidRequest_Success()
        {
            // Arrange
            var profile = new Profile(Guid.NewGuid());
            var request = new ProfileUpdateRequest
            {
                DisplayName = "Test Teacher",
                InstructorType = "Professor"
            };

            // Act
            var result = _strategy.UpdateProfile(profile, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Professor", profile.InstructorType);
        }
    }
}
