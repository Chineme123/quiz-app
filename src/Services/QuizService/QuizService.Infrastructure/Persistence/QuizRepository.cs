using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuizService.Domain.Entities;
using QuizService.Domain.Interfaces;

namespace QuizService.Infrastructure.Persistence
{
    public class QuizRepository : IQuizRepository
    {
        private readonly QuizDbContext _context;

        public QuizRepository(QuizDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Quiz quiz)
        {
            await _context.Quizzes.AddAsync(quiz);
            await _context.SaveChangesAsync();
        }

        public async Task<Quiz> GetByIdAsync(Guid id)
        {
            return await _context.Quizzes
                         .Include(q => q.Questions)
                         .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task UpdateAsync(Quiz quiz)
        {
            _context.Quizzes.Update(quiz);
            await _context.SaveChangesAsync();
        }

        public async Task<Classroom> GetClassroomAsync(Guid classroomId)
        {
            return await _context.Classrooms.FindAsync(classroomId);
        }
    }
}
