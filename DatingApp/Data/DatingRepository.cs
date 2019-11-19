using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.Helpers;
using DatingApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.Data {
    public class DatingRepository : IDatingRepository {
        private readonly DataContext _context;
        public DatingRepository (DataContext context) {
            _context = context;
        }
        public void Add<T> (T entity) where T : class {
            _context.Add (entity);
        }

        public void Delete<T> (T entity) where T : class {
            _context.Remove (entity);
        }

        public async Task<User> GetUser (int userId) {
            return await _context.Users.Include (p => p.photos).FirstOrDefaultAsync (p => p.Id == userId);
        }

        public async Task<PagedList<User>> GetUsers (UserParams userParams) {
            var users = _context.Users.Include (p => p.photos).OrderByDescending (u => u.LastActive).AsQueryable ();

            users = users.Where (u => u.Id != userParams.UserId);
            users = users.Where (u => u.Gender == userParams.Gender);

            if (userParams.Likers) {
                var userLikers = await GetUserLikes (userParams.UserId, userParams.Likers);
                users = users.Where (u => userLikers.Contains (u.Id));
            }

            if (userParams.Likees) {
                var userLikees = await GetUserLikes (userParams.UserId, userParams.Likers);
                users = users.Where (u => userLikees.Contains (u.Id));
            }

            if (userParams.MinAge != 18 || userParams.MaxAge != 99) {
                var minDob = DateTime.Today.AddYears (-userParams.MaxAge - 1);
                var maxDob = DateTime.Today.AddYears (-userParams.MinAge);

                users = users.Where (u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
            }

            if (!string.IsNullOrEmpty (userParams.OrderBy)) {
                switch (userParams.OrderBy) {
                    case "created":
                        users = users.OrderByDescending (u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending (u => u.LastActive);
                        break;
                }
            }

            return await PagedList<User>.CreateAsync (users, userParams.PageNumber, userParams.PageSize);
        }

        private async Task<IEnumerable<int>> GetUserLikes (int userId, bool likers) {
            var user = await _context.Users.Include (x => x.Likers).Include (x => x.Likees).FirstOrDefaultAsync (u => u.Id == userId);

            if (likers) {
                return user.Likers.Where (u => u.LikeeId == userId).Select (i => i.LikerId);
            } else {
                return user.Likees.Where (u => u.LikerId == userId).Select (i => i.LikeeId);
            }
        }

        public async Task<bool> SaveAll () {
            return await _context.SaveChangesAsync () > 0;
        }

        public async Task<Photo> GetPhoto (int photoId) {
            return await _context.photos.FirstOrDefaultAsync (p => p.Id == photoId);
        }

        public async Task<Photo> GetMainPhotoForUser (int userId) {
            return await _context.photos.Where (u => u.UserId == userId).FirstOrDefaultAsync (p => p.IsMain);
        }

        public async Task<Like> GetLike (int userId, int recipientId) {
            return await _context.Likes.FirstOrDefaultAsync (u => u.LikerId == userId && u.LikeeId == recipientId);
        }

        public async Task<Message> GetMessage (int messageId) {
            return await _context.Messages.FirstOrDefaultAsync (p => p.Id == messageId);
        }

        public Task<PagedList<Message>> GetMessagesForUser () {
            throw new NotImplementedException ();
        }

        public async Task<IEnumerable<Message>> GetMessageThread (int userId, int recipientId) {
            var message = await _context.Messages
                .Include (p => p.Sender).ThenInclude (p => p.photos)
                .Include (p => p.Recipient).ThenInclude (p => p.photos)
                .Where (m => m.RecipientId == userId && m.SenderId == recipientId ||
                    m.RecipientId == recipientId && m.SenderId == userId)
                .OrderByDescending (m => m.MessageSent)
                .ToListAsync ();

            return message;
        }

        public async Task<PagedList<Message>> GetMessagesForUser (MessageParams messageParams) {
            var messages = _context.Messages.Include (u => u.Sender).ThenInclude (p => p.photos)
                .Include (u => u.Recipient).ThenInclude (p => p.photos).AsQueryable ();

            switch (messageParams.MessageContaienr) {
                case "Inbox":
                    messages = messages.Where (u => u.RecipientId == messageParams.UserId);
                    break;

                case "Outbox":
                    messages = messages.Where (u => u.SenderId == messageParams.UserId);
                    break;
                default:
                    messages = messages.Where (u => u.RecipientId == messageParams.UserId && u.IsRead == false);
                    break;
            }

            messages = messages.OrderByDescending (p => p.MessageSent);
            return await PagedList<Message>.CreateAsync (messages, messageParams.PageNumber, messageParams.PageSize);
        }
    }
}