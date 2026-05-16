import type { UserStories } from '../types';
import { Avatar } from './Avatar';

export function StoryRing({ userStories, onClick }: { userStories: UserStories; onClick: () => void }) {
  return (
    <button onClick={onClick} className="flex flex-col items-center gap-1 min-w-[64px]">
      <Avatar user={userStories.user} size="md" ring />
      <span className="text-xs text-gray-600 truncate max-w-[60px]">{userStories.user.displayName.split(' ')[0]}</span>
    </button>
  );
}
