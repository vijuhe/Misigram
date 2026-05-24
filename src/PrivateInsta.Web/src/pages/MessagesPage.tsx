import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { formatDistanceToNow } from 'date-fns';
import { PencilSquareIcon, XMarkIcon } from '@heroicons/react/24/outline';
import { chats as chatsApi, users as usersApi } from '../api';
import { useAuth } from '../AuthContext';
import { Avatar } from '../components/Avatar';
import type { ChatGroup, User } from '../types';

export function MessagesPage() {
  const { user: me } = useAuth();
  const qc = useQueryClient();
  const [showNew, setShowNew] = useState(false);
  const [selected, setSelected] = useState<User[]>([]);
  const [groupName, setGroupName] = useState('');
  const [creating, setCreating] = useState(false);

  const navigate = useNavigate();
  const { data: chatList } = useQuery({ queryKey: ['chats'], queryFn: chatsApi.list });
  const { data: allUsers } = useQuery({ queryKey: ['users'], queryFn: usersApi.list, enabled: showNew });

  const otherUsers = allUsers?.filter(u => u.id !== me?.id) ?? [];

  const chatName = (g: ChatGroup) => {
    if (g.name) return g.name;
    const others = g.members.filter(m => m.id !== me?.id);
    return others.map(m => m.displayName).join(', ') || 'Chat';
  };

  const handleCreate = async () => {
    if (selected.length === 0) return;

    // For 1:1 DMs reuse an existing thread rather than creating a duplicate
    if (selected.length === 1) {
      const existing = chatList?.find(g =>
        g.name === null &&
        g.members.length === 2 &&
        g.members.some(m => m.id === selected[0].id)
      );
      if (existing) {
        setShowNew(false);
        setSelected([]);
        navigate(`/messages/${existing.id}`);
        return;
      }
    }

    setCreating(true);
    const name = selected.length > 1 ? (groupName || null) : null;
    const newChat = await chatsApi.create(name, selected.map(u => u.id));
    qc.invalidateQueries({ queryKey: ['chats'] });
    setShowNew(false);
    setSelected([]);
    setGroupName('');
    setCreating(false);
    navigate(`/messages/${newChat.id}`);
  };

  const toggleUser = (u: User) =>
    setSelected(s => s.find(x => x.id === u.id) ? s.filter(x => x.id !== u.id) : [...s, u]);

  return (
    <div className="py-4">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-bold text-gray-900">Messages</h2>
        <button onClick={() => setShowNew(true)} className="text-pink-600">
          <PencilSquareIcon className="w-6 h-6" />
        </button>
      </div>

      {chatList?.length === 0 && <p className="text-gray-400 text-sm">No conversations yet.</p>}

      <div className="space-y-1">
        {chatList?.map(g => (
          <Link key={g.id} to={`/messages/${g.id}`} className="flex items-center gap-3 bg-white rounded-xl border border-gray-200 p-3 hover:bg-gray-50">
            <div className="w-10 h-10 rounded-full bg-gradient-to-br from-pink-400 to-purple-500 flex items-center justify-center text-white font-bold text-sm shrink-0">
              {chatName(g).slice(0, 2).toUpperCase()}
            </div>
            <div className="flex-1 min-w-0">
              <p className="font-semibold text-sm text-gray-900 truncate">{chatName(g)}</p>
              {g.lastMessage && (
                <p className="text-xs text-gray-400 truncate">
                  {g.lastMessage.sender.displayName}: {g.lastMessage.content}
                </p>
              )}
            </div>
            {g.lastMessage && (
              <span className="text-xs text-gray-300 shrink-0">
                {formatDistanceToNow(new Date(g.lastMessage.createdAt), { addSuffix: true })}
              </span>
            )}
          </Link>
        ))}
      </div>

      {/* New chat modal */}
      {showNew && (
        <div className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl w-full max-w-sm shadow-2xl">
            <div className="flex items-center justify-between p-4 border-b">
              <h3 className="font-semibold text-gray-900">New Conversation</h3>
              <button onClick={() => setShowNew(false)}><XMarkIcon className="w-5 h-5 text-gray-500" /></button>
            </div>
            <div className="p-4 space-y-3">
              {selected.length > 1 && (
                <input
                  value={groupName}
                  onChange={e => setGroupName(e.target.value)}
                  placeholder="Group name (optional)"
                  className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none"
                />
              )}
              <div className="space-y-1 max-h-64 overflow-y-auto">
                {otherUsers.map(u => (
                  <button key={u.id} onClick={() => toggleUser(u)} className={`flex items-center gap-3 w-full p-2 rounded-lg text-left ${selected.find(x => x.id === u.id) ? 'bg-pink-50' : 'hover:bg-gray-50'}`}>
                    <Avatar user={u} size="sm" />
                    <span className="text-sm font-medium">{u.displayName}</span>
                    {selected.find(x => x.id === u.id) && <span className="ml-auto text-pink-500 text-xs font-medium">✓</span>}
                  </button>
                ))}
              </div>
            </div>
            <div className="p-4 border-t flex justify-end">
              <button onClick={handleCreate} disabled={selected.length === 0 || creating}
                className="bg-pink-600 text-white text-sm font-semibold px-6 py-2 rounded-full disabled:opacity-40 hover:bg-pink-700">
                {creating ? 'Creating…' : selected.length > 1 ? 'Create Group' : 'Start Chat'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
