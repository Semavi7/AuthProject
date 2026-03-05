namespace AuthProject.Entites
{
    public class ForgetPassword
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid VerifyId { get; set; }
        public DateTime ExpireAt { get; set; }
        public DateTime? IsUsedAt { get; set; }
        public User User { get; set; }
        public Verify Verify { get; set; }
    }
}
