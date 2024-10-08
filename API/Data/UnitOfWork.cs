﻿using API.Interfaces;

namespace API.Data;

public class UnitOfWork(DataContext context,
    IUserRepository userRepository,
    ILikesRepository likesRepository,
    IMessagesRepository messagesRepository) : IUnitOfWork
{
    public IUserRepository UserRepository => userRepository;

    public IMessagesRepository MessagesRepository => messagesRepository;

    public ILikesRepository LikesRepository => likesRepository;

    public async Task<bool> CompleteAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }

    public bool HasChanges()
    {
        return context.ChangeTracker.HasChanges();
    }
}
