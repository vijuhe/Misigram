import axios from 'axios';
import type { ChatGroup, Comment, Message, PagedResult, Post, User, UserStories } from './types';

const http = axios.create({ baseURL: '/api', withCredentials: true });

export const auth = {
  me: () => http.get<User>('/auth/me').then(r => r.data),
  logout: () => http.post('/auth/logout'),
};

export const posts = {
  feed: (page = 1, size = 20) =>
    http.get<PagedResult<Post>>('/posts', { params: { page, size } }).then(r => r.data),
  get: (id: string) => http.get<Post>(`/posts/${id}`).then(r => r.data),
  create: (data: FormData) => http.post<Post>('/posts', data).then(r => r.data),
  delete: (id: string) => http.delete(`/posts/${id}`),
  comments: (id: string) => http.get<Comment[]>(`/posts/${id}/comments`).then(r => r.data),
  addComment: (id: string, content: string) =>
    http.post<Comment>(`/posts/${id}/comments`, { content }).then(r => r.data),
  deleteComment: (postId: string, commentId: string) =>
    http.delete(`/posts/${postId}/comments/${commentId}`),
  toggleLike: (id: string) =>
    http.post<{ liked: boolean }>(`/posts/${id}/like`).then(r => r.data),
};

export const stories = {
  list: () => http.get<UserStories[]>('/stories').then(r => r.data),
  create: (data: FormData) => http.post('/stories', data),
  delete: (id: string) => http.delete(`/stories/${id}`),
};

export const users = {
  list: () => http.get<User[]>('/users').then(r => r.data),
  get: (id: string) => http.get<User>(`/users/${id}`).then(r => r.data),
  me: () => http.get<User>('/users/me').then(r => r.data),
  posts: (id: string, page = 1, size = 20) =>
    http.get<PagedResult<Post>>(`/users/${id}/posts`, { params: { page, size } }).then(r => r.data),
  update: (data: { displayName: string; bio?: string }) =>
    http.put<User>('/users/me', data).then(r => r.data),
  uploadAvatar: (file: File) => {
    const form = new FormData();
    form.append('file', file);
    return http.post<User>('/users/me/avatar', form).then(r => r.data);
  },
};

export const chats = {
  list: () => http.get<ChatGroup[]>('/chats').then(r => r.data),
  get: (id: string) => http.get<ChatGroup>(`/chats/${id}`).then(r => r.data),
  create: (name: string | null, memberIds: string[]) =>
    http.post<ChatGroup>('/chats', { name, memberIds }).then(r => r.data),
  messages: (id: string, page = 1, size = 50) =>
    http.get<PagedResult<Message>>(`/chats/${id}/messages`, { params: { page, size } }).then(r => r.data),
  addMember: (chatId: string, userId: string) =>
    http.post(`/chats/${chatId}/members`, { userId }),
};
