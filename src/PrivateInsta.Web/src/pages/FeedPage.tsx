import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { posts as postsApi, stories as storiesApi } from '../api';
import { PostCard } from '../components/PostCard';
import { StoryRing } from '../components/StoryRing';
import { StoryViewer } from '../components/StoryViewer';
import type { Story } from '../types';

export function FeedPage() {
  const [viewerStories, setViewerStories] = useState<Story[] | null>(null);

  const { data: feed } = useQuery({ queryKey: ['feed'], queryFn: () => postsApi.feed() });
  const { data: storiesData } = useQuery({ queryKey: ['stories'], queryFn: storiesApi.list });

  return (
    <div className="py-4">
      {/* Stories row */}
      {storiesData && storiesData.length > 0 && (
        <div className="flex gap-4 overflow-x-auto pb-4 mb-4 scrollbar-none">
          {storiesData.map(us => (
            <StoryRing
              key={us.user.id}
              userStories={us}
              onClick={() => setViewerStories(us.stories)}
            />
          ))}
        </div>
      )}

      {/* Feed */}
      {feed?.items.map(post => <PostCard key={post.id} post={post} />)}
      {feed?.items.length === 0 && (
        <div className="text-center text-gray-400 py-20">
          <p className="text-lg font-medium">Nothing yet</p>
          <p className="text-sm mt-1">Be the first to share a post!</p>
        </div>
      )}

      {viewerStories && (
        <StoryViewer stories={viewerStories} onClose={() => setViewerStories(null)} />
      )}
    </div>
  );
}
