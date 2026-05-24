import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { users as usersApi } from '../api';
import { Avatar } from '../components/Avatar';
import { useAuth } from '../AuthContext';

export function ProfilePage() {
  const { id } = useParams<{ id: string }>();
  const { user: me } = useAuth();

  const { data: user } = useQuery({ queryKey: ['user', id], queryFn: () => usersApi.get(id!), enabled: !!id });
  const { data: postsData } = useQuery({ queryKey: ['userPosts', id], queryFn: () => usersApi.posts(id!), enabled: !!id });

  const userPosts = postsData?.items ?? [];

  if (!user) return null;

  return (
    <div className="py-4">
      <div className="bg-white rounded-xl border border-gray-200 p-5 mb-4">
        <div className="flex items-center gap-4 mb-3">
          <Avatar user={user} size="lg" />
          <div className="flex-1 min-w-0">
            <h2 className="font-bold text-lg text-gray-900 truncate">{user.displayName}</h2>
            <p className="text-sm text-gray-400 truncate">{user.email}</p>
          </div>
          {me?.id === user.id && (
            <Link to="/profile/me" className="ml-auto shrink-0 text-sm border border-gray-300 px-3 py-1 rounded-full text-gray-700 hover:bg-gray-50">Edit</Link>
          )}
        </div>
        {user.bio && <p className="text-sm text-gray-700">{user.bio}</p>}
      </div>

      {/* Posts grid */}
      <div className="grid grid-cols-3 gap-1">
        {userPosts.map(post => (
          <Link key={post.id} to={`/post/${post.id}`}>
            <img
              src={post.thumbnailUrl ?? post.mediaUrl}
              alt={post.caption ?? ''}
              className="w-full aspect-square object-cover rounded"
            />
          </Link>
        ))}
      </div>

      {userPosts.length === 0 && (
        <p className="text-center text-gray-400 text-sm py-8">No posts yet.</p>
      )}
    </div>
  );
}
