using Crowbar.Data;

namespace Crowbar.Actions
{
    public partial class ForumActions
    {
        public readonly ApplicationDbContext _context;
        public ForumActions(ApplicationDbContext context) {
            _context = context;   
        }
    }
}
