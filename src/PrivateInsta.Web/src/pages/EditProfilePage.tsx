import { useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../AuthContext';
import { users as usersApi } from '../api';
import { Avatar } from '../components/Avatar';

export function EditProfilePage() {
  const { user, refetch } = useAuth();
  const navigate = useNavigate();
  const [displayName, setDisplayName] = useState(user?.displayName ?? '');
  const [bio, setBio] = useState(user?.bio ?? '');
  const [saving, setSaving] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  if (!user) return null;

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    await usersApi.update({ displayName, bio: bio || undefined });
    await refetch();
    setSaving(false);
    navigate(`/profile/${user.id}`);
  };

  const handleAvatar = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    await usersApi.uploadAvatar(file);
    refetch();
  };

  return (
    <div className="py-4">
      <div className="bg-white rounded-xl border border-gray-200 p-5">
        <h2 className="font-bold text-lg text-gray-900 mb-4">Edit Profile</h2>

        <div className="flex flex-col items-center mb-6">
          <Avatar user={user} size="lg" />
          <button onClick={() => fileRef.current?.click()} className="mt-2 text-sm text-pink-600 font-medium">Change photo</button>
          <input ref={fileRef} type="file" accept="image/*" className="hidden" onChange={handleAvatar} />
        </div>

        <form onSubmit={handleSave} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-gray-500 mb-1">Display Name</label>
            <input
              value={displayName}
              onChange={e => setDisplayName(e.target.value)}
              required
              className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-pink-300"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-500 mb-1">Bio</label>
            <textarea
              value={bio}
              onChange={e => setBio(e.target.value)}
              rows={3}
              className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-pink-300"
            />
          </div>
          <button type="submit" disabled={saving} className="w-full bg-pink-600 text-white font-semibold py-2 rounded-full disabled:opacity-40 hover:bg-pink-700">
            {saving ? 'Saving…' : 'Save Changes'}
          </button>
        </form>
      </div>
    </div>
  );
}
