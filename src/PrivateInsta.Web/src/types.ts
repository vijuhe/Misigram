export type MediaType = 'Photo' | 'Video';

export interface User {
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  bio: string | null;
  createdAt: string;
}

export interface Post {
  id: string;
  author: User;
  mediaUrl: string;
  thumbnailUrl: string | null;
  mediaType: MediaType;
  caption: string | null;
  likeCount: number;
  commentCount: number;
  likedByMe: boolean;
  createdAt: string;
}

export interface Comment {
  id: string;
  author: User;
  content: string;
  createdAt: string;
}

export interface Story {
  id: string;
  author: User;
  mediaUrl: string;
  mediaType: MediaType;
  createdAt: string;
  expiresAt: string;
}

export interface UserStories {
  user: User;
  stories: Story[];
}

export interface ChatGroup {
  id: string;
  name: string | null;
  members: User[];
  lastMessage: Message | null;
  createdAt: string;
}

export interface Message {
  id: string;
  chatGroupId: string;
  sender: User;
  content: string;
  mediaUrl: string | null;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
