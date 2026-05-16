import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { stories as storiesApi } from '../api';
import { StoryRing } from '../components/StoryRing';
import { StoryViewer } from '../components/StoryViewer';
import type { Story } from '../types';

export function StoriesPage() {
  const [viewerStories, setViewerStories] = useState<Story[] | null>(null);
  const { data } = useQuery({ queryKey: ['stories'], queryFn: storiesApi.list });

  return (
    <div className="py-4">
      <h2 className="text-lg font-bold text-gray-900 mb-4">Stories</h2>
      {data && data.length > 0
        ? <div className="flex flex-wrap gap-6">
            {data.map(us => (
              <StoryRing key={us.user.id} userStories={us} onClick={() => setViewerStories(us.stories)} />
            ))}
          </div>
        : <p className="text-gray-400 text-sm">No active stories right now.</p>}

      {viewerStories && <StoryViewer stories={viewerStories} onClose={() => setViewerStories(null)} />}
    </div>
  );
}
