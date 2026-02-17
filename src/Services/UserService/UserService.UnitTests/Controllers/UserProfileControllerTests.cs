using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using UserService.API.Controllers;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Domain.Models;
using Xunit;

namespace UserService.UnitTests.Controllers
{
    public class UserProfileControllerTests
    {
        private readonly Mock<IProfileRepository> _mockRepo;
        private readonly Mock<IProfileUpdateStrategy> _mockStrategy;
        private readonly UserProfileController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public UserProfileControllerTests()
        {
            _mockRepo = new Mock<IProfileRepository>();
            _mockStrategy = new Mock<IProfileUpdateStrategy>();

            // Setup strategy to support "Student"
            _mockStrategy.Setup(s => s.Supports("Student")).Returns(true);

            var strategies = new List<IProfileUpdateStrategy> { _mockStrategy.Object };

            _controller = new UserProfileController(_mockRepo.Object, strategies);

            // Mock User context
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                new Claim(ClaimTypes.Role, "Student")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task UpdateProfile_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new ProfileUpdateRequest { DisplayName = "Test" };
            _mockRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync((Profile?)null); // New profile logic
            _mockStrategy.Setup(s => s.UpdateProfile(It.IsAny<Profile>(), request))
                         .Returns(ValidationResult.Success());

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Profile>()), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateProfile_StrategyValidationFails_ReturnsBadRequest()
        {
            // Arrange
            var request = new ProfileUpdateRequest { DisplayName = "Test" };
            _mockRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(new Profile(_userId));
            _mockStrategy.Setup(s => s.UpdateProfile(It.IsAny<Profile>(), request))
                         .Returns(ValidationResult.Fail("Role error"));

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_NoStrategyFound_ReturnsBadRequest()
        {
            // Arrange: Force unsupported role context
             var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                new Claim(ClaimTypes.Role, "UnknownRole")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            var request = new ProfileUpdateRequest();

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No profile strategy found for role: UnknownRole", actionResult.Value);
        }
    }
}
