import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { HeartIcon, ChatBubbleOvalLeftIcon } from '@heroicons/react/24/outline';
import { HeartIcon as HeartSolidIcon } from '@heroicons/react/24/solid';
import { formatDistanceToNow } from 'date-fns';
import { posts as postsApi } from '../api';
import type { PagedResult, Post } from '../types';
import { Avatar } from './Avatar';

export function PostCard({ post }: { post: Post }) {
  const qc = useQueryClient();
  const [liking, setLiking] = useState(false);

  const handleLike = async () => {
    if (liking) return;
    setLiking(true);
    try {
      const result = await postsApi.toggleLike(post.id);
      qc.setQueryData<PagedResult<Post>>(['feed'], old =>
        old ? {
          ...old,
          items: old.items.map(p => p.id === post.id
            ? { ...p, likedByMe: result.liked, likeCount: result.liked ? p.likeCount + 1 : p.likeCount - 1 }
            : p
          ),
        } : old
      );
    } finally {
      setLiking(false);
    }
  };

  return (
    <article className="bg-white border border-gray-200 rounded-xl overflow-hidden mb-4">
      <div className="flex items-center gap-3 p-3">
        <Link to={`/profile/${post.author.id}`}>
          <Avatar user={post.author} size="sm" />
        </Link>
        <div className="flex-1 min-w-0">
          <Link to={`/profile/${post.author.id}`} className="font-semibold text-sm text-gray-900 hover:underline">
            {post.author.displayName}
          </Link>
          <p className="text-xs text-gray-400">{formatDistanceToNow(new Date(post.createdAt), { addSuffix: true })}</p>
        </div>
      </div>

      {post.mediaUrl && (
        <Link to={`/post/${post.id}`}>
          {post.mediaType === 'Video'
            ? <video src={post.mediaUrl} className="w-full max-h-[500px] object-cover bg-black" controls muted />
            : <img src={post.thumbnailUrl ?? post.mediaUrl} alt={post.caption ?? ''} className="w-full h-auto block" />
          }
        </Link>
      )}

      <div className="p-3">
        {post.caption && <p className="text-sm text-gray-800 mb-2"><span className="font-semibold mr-1">{post.author.displayName}</span>{post.caption}</p>}
        <div className="flex items-center gap-4">
          <button onClick={handleLike} disabled={liking} className="flex items-center gap-1 text-sm">
            {post.likedByMe
              ? <HeartSolidIcon className="w-6 h-6 text-red-500" />
              : <HeartIcon className="w-6 h-6 text-gray-600" />}
            <span className="text-gray-700">{post.likeCount}</span>
          </button>
          <Link to={`/post/${post.id}`} className="flex items-center gap-1 text-sm text-gray-600">
            <ChatBubbleOvalLeftIcon className="w-6 h-6" />
            <span>{post.commentCount}</span>
          </Link>
        </div>
      </div>
    </article>
  );
}
