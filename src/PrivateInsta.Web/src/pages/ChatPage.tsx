import { useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import * as signalR from '@microsoft/signalr';
import { formatDistanceToNow } from 'date-fns';
import { PaperAirplaneIcon } from '@heroicons/react/24/solid';
import { chats as chatsApi } from '../api';
import { useAuth } from '../AuthContext';
import { Avatar } from '../components/Avatar';
import type { Message } from '../types';

export function ChatPage() {
  const { chatId } = useParams<{ chatId: string }>();
  const { user: me } = useAuth();
  const qc = useQueryClient();
  const [messages, setMessages] = useState<Message[]>([]);
  const [text, setText] = useState('');
  const [connected, setConnected] = useState(false);
  const bottomRef = useRef<HTMLDivElement>(null);
  const hubRef = useRef<signalR.HubConnection | null>(null);

  const { data: chat } = useQuery({
    queryKey: ['chat', chatId],
    queryFn: () => chatsApi.get(chatId!),
    enabled: !!chatId,
  });

  // Load message history
  useEffect(() => {
    if (!chatId) return;
    chatsApi.messages(chatId).then(r => setMessages([...r.items].reverse()));
  }, [chatId]);

  // SignalR connection
  useEffect(() => {
    if (!chatId) return;

    const hub = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat', { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    hub.on('ReceiveMessage', (msg: Message) => {
      setMessages(prev => [...prev, msg]);
      qc.invalidateQueries({ queryKey: ['chats'] });
    });

    const joinGroup = () => hub.invoke('JoinGroup', chatId).catch(console.error);

    hub.onreconnected(() => { setConnected(true); joinGroup(); });
    hub.onreconnecting(() => setConnected(false));
    hub.onclose(() => setConnected(false));

    hub.start()
      .then(() => { setConnected(true); return joinGroup(); })
      .catch(console.error);

    hubRef.current = hub;
    return () => {
      hub.stop();
      hubRef.current = null;
      setConnected(false);
    };
  }, [chatId]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const sendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!text.trim() || !hubRef.current || !connected) return;
    try {
      await hubRef.current.invoke('SendMessage', chatId, text.trim());
      setText('');
    } catch (err) {
      console.error('Failed to send message:', err);
    }
  };

  const chatTitle = () => {
    if (!chat) return '…';
    if (chat.name) return chat.name;
    return chat.members.filter(m => m.id !== me?.id).map(m => m.displayName).join(', ') || 'Chat';
  };

  return (
    <div className="flex flex-col h-[calc(100vh-7rem)]">
      <div className="bg-white border-b border-gray-200 py-3 sticky top-14 z-10">
        <p className="font-semibold text-gray-900 text-center">{chatTitle()}</p>
      </div>

      <div className="flex-1 overflow-y-auto py-4 space-y-3">
        {messages.map(msg => {
          const mine = msg.sender.id === me?.id;
          return (
            <div key={msg.id} className={`flex items-end gap-2 ${mine ? 'flex-row-reverse' : ''}`}>
              {!mine && <Avatar user={msg.sender} size="sm" />}
              <div className={`max-w-[70%] ${mine ? 'items-end' : 'items-start'} flex flex-col`}>
                {!mine && <span className="text-xs text-gray-400 mb-0.5 ml-1">{msg.sender.displayName}</span>}
                <div className={`px-4 py-2 rounded-2xl text-sm ${mine ? 'bg-pink-600 text-white rounded-br-sm' : 'bg-white border border-gray-200 text-gray-900 rounded-bl-sm'}`}>
                  {msg.content}
                </div>
                <span className="text-xs text-gray-300 mt-0.5 mx-1">
                  {formatDistanceToNow(new Date(msg.createdAt), { addSuffix: true })}
                </span>
              </div>
            </div>
          );
        })}
        <div ref={bottomRef} />
      </div>

      {!connected && (
        <p className="text-center text-xs text-gray-400 py-1 bg-white border-t border-gray-100">Connecting…</p>
      )}
      <form onSubmit={sendMessage} className="bg-white border-t border-gray-200 flex items-center gap-2 px-4 py-3">
        <input
          value={text}
          onChange={e => setText(e.target.value)}
          placeholder="Message…"
          disabled={!connected}
          className="flex-1 bg-gray-100 rounded-full px-4 py-2 text-sm outline-none disabled:opacity-50"
        />
        <button type="submit" disabled={!text.trim() || !connected} className="text-pink-600 disabled:opacity-40">
          <PaperAirplaneIcon className="w-6 h-6" />
        </button>
      </form>
    </div>
  );
}
