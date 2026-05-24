import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { formatDistanceToNow } from 'date-fns';
import { HeartIcon, TrashIcon } from '@heroicons/react/24/outline';
import { HeartIcon as HeartSolidIcon } from '@heroicons/react/24/solid';
import { posts as postsApi } from '../api';
import { useAuth } from '../AuthContext';
import { Avatar } from '../components/Avatar';
import { Spinner } from '../components/Spinner';
import type { PagedResult, Post } from '../types';

export function PostDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const qc = useQueryClient();
  const [comment, setComment] = useState('');

  const [liking, setLiking] = useState(false);

  const { data: post, isLoading, refetch } = useQuery({
    queryKey: ['post', id],
    queryFn: () => postsApi.get(id!),
    enabled: !!id,
  });

  const { data: comments, refetch: refetchComments } = useQuery({
    queryKey: ['comments', id],
    queryFn: () => postsApi.comments(id!),
    enabled: !!id,
  });

  if (isLoading) return <Spinner />;
  if (!post) return null;

  const handleLike = async () => {
    if (liking) return;
    setLiking(true);
    const optimistic: Post = {
      ...post,
      likedByMe: !post.likedByMe,
      likeCount: post.likedByMe ? post.likeCount - 1 : post.likeCount + 1,
    };
    qc.setQueryData(['post', id], optimistic);
    qc.setQueryData<PagedResult<Post>>(['feed'], old =>
      old ? { ...old, items: old.items.map(p => p.id === post.id ? optimistic : p) } : old
    );
    try {
      await postsApi.toggleLike(post.id);
    } catch {
      qc.setQueryData(['post', id], post);
      qc.invalidateQueries({ queryKey: ['feed'] });
    } finally {
      setLiking(false);
    }
  };

  const handleComment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!comment.trim()) return;
    await postsApi.addComment(post.id, comment.trim());
    setComment('');
    refetchComments();
    refetch();
    qc.invalidateQueries({ queryKey: ['feed'] });
  };

  const handleDeleteComment = async (commentId: string) => {
    await postsApi.deleteComment(post.id, commentId);
    refetchComments();
    refetch();
    qc.invalidateQueries({ queryKey: ['feed'] });
  };

  return (
    <div className="py-4">
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        {/* Header */}
        <div className="flex items-center gap-3 p-3">
          <Link to={`/profile/${post.author.id}`}><Avatar user={post.author} size="sm" /></Link>
          <div>
            <Link to={`/profile/${post.author.id}`} className="font-semibold text-sm">{post.author.displayName}</Link>
            <p className="text-xs text-gray-400">{formatDistanceToNow(new Date(post.createdAt), { addSuffix: true })}</p>
          </div>
        </div>

        {/* Media */}
        {post.mediaUrl && (post.mediaType === 'Video'
          ? <video src={post.mediaUrl} className="w-full max-h-[500px] object-cover bg-black" controls />
          : <img src={post.mediaUrl} alt={post.caption ?? ''} className="w-full h-auto block" />
        )}

        {/* Actions */}
        <div className="p-3">
          {post.caption && <p className="text-sm mb-2"><span className="font-semibold mr-1">{post.author.displayName}</span>{post.caption}</p>}
          <div className="flex items-center gap-4">
            <button onClick={handleLike} className="flex items-center gap-1 text-sm">
              {post.likedByMe
                ? <HeartSolidIcon className="w-6 h-6 text-red-500" />
                : <HeartIcon className="w-6 h-6 text-gray-600" />}
              <span>{post.likeCount}</span>
            </button>
          </div>
        </div>

        {/* Comments */}
        <div className="border-t px-3 py-2 space-y-2 max-h-64 overflow-y-auto">
          {comments?.map(c => (
            <div key={c.id} className="flex items-start gap-2 group">
              <Avatar user={c.author} size="sm" />
              <div className="flex-1 min-w-0">
                <span className="font-semibold text-xs mr-1">{c.author.displayName}</span>
                <span className="text-sm text-gray-800">{c.content}</span>
                <p className="text-xs text-gray-400">{formatDistanceToNow(new Date(c.createdAt), { addSuffix: true })}</p>
              </div>
              {c.author.id === user?.id && (
                <button onClick={() => handleDeleteComment(c.id)} className="opacity-0 group-hover:opacity-100 text-gray-400 hover:text-red-500">
                  <TrashIcon className="w-4 h-4" />
                </button>
              )}
            </div>
          ))}
        </div>

        {/* Add comment */}
        <form onSubmit={handleComment} className="border-t flex items-center gap-2 p-3">
          <input
            value={comment}
            onChange={e => setComment(e.target.value)}
            placeholder="Add a comment…"
            className="flex-1 text-sm outline-none"
          />
          <button type="submit" disabled={!comment.trim()} className="text-sm font-semibold text-pink-600 disabled:opacity-40">Post</button>
        </form>
      </div>
    </div>
  );
}
