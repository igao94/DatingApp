namespace API.Interfaces;

public interface IUnitOfWork
{
    IUserRepository UserRepository { get; }
    IMessagesRepository MessagesRepository { get; }
    ILikesRepository LikesRepository { get; }
    Task<bool> CompleteAsync();
    bool HasChanges();
}
