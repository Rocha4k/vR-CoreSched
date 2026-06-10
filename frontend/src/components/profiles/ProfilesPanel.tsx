import { useEffect, useState } from 'react';
import type { CurrentUser, UpdateProfileRequest, UpsertUserRequest, UserProfile } from '../../lib/types';

type Props = {
  currentUser: CurrentUser;
  users: UserProfile[];
  canManageUsers: boolean;
  onUpdateProfile: (request: UpdateProfileRequest) => Promise<UserProfile>;
  onCreateUser: (request: UpsertUserRequest) => Promise<UserProfile>;
  onSaveUser: (request: UpsertUserRequest) => Promise<UserProfile>;
};

export function ProfilesPanel({ currentUser, users, canManageUsers, onUpdateProfile, onCreateUser, onSaveUser }: Props) {
  const [profileDraft, setProfileDraft] = useState<UpdateProfileRequest>({ fullName: currentUser.fullName, currentPassword: '', newPassword: '' });
  const [userDrafts, setUserDrafts] = useState<UserProfile[]>(users);
  const [newUser, setNewUser] = useState<UpsertUserRequest>({ username: '', fullName: '', role: 'Operator', isActive: true, password: '' });
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    setProfileDraft({ fullName: currentUser.fullName, currentPassword: '', newPassword: '' });
  }, [currentUser.fullName]);

  useEffect(() => {
    setUserDrafts(users);
  }, [users]);

  const handleSaveProfile = async () => {
    const saved = await onUpdateProfile(profileDraft);
    setProfileDraft({ fullName: saved.fullName, currentPassword: '', newPassword: '' });
    setMessage('Perfil atualizado.');
  };

  const handleSaveUser = async (user: UserProfile) => {
    const saved = await onSaveUser({
      username: user.username,
      fullName: user.fullName,
      role: user.role,
      isActive: user.isActive,
      password: null
    });

    setUserDrafts(current => current.map(item => item.username === saved.username ? saved : item));
    setMessage(`Utilizador ${saved.username} atualizado.`);
  };

  const handleCreateUser = async () => {
    const saved = await onCreateUser(newUser);
    setUserDrafts(current => [...current.filter(item => item.username !== saved.username), saved].sort((left, right) => left.fullName.localeCompare(right.fullName)));
    setNewUser({ username: '', fullName: '', role: 'Operator', isActive: true, password: '' });
    setMessage(`Utilizador ${saved.username} criado.`);
  };

  return (
    <section className="panel profiles-panel">
      <div className="panel-header">
        <h2>Perfis</h2>
        <span>{currentUser.role}</span>
      </div>

      <div className="profiles-grid">
        <article className="profile-card">
          <div className="profile-card__head">
            <div>
              <strong>{currentUser.fullName}</strong>
              <p>{currentUser.username}</p>
            </div>
            <span>{currentUser.isActive ? 'Ativo' : 'Inativo'}</span>
          </div>
          <label>
            Nome completo
            <input value={profileDraft.fullName} onChange={event => setProfileDraft(current => ({ ...current, fullName: event.target.value }))} />
          </label>
          <label>
            Password atual
            <input type="password" value={profileDraft.currentPassword ?? ''} onChange={event => setProfileDraft(current => ({ ...current, currentPassword: event.target.value }))} />
          </label>
          <label>
            Nova password
            <input type="password" value={profileDraft.newPassword ?? ''} onChange={event => setProfileDraft(current => ({ ...current, newPassword: event.target.value }))} />
          </label>
          <button type="button" onClick={() => void handleSaveProfile()}>Guardar perfil</button>
        </article>

        {canManageUsers ? (
          <article className="profile-card profile-card--create">
            <div className="profile-card__head">
              <div>
                <strong>Novo utilizador</strong>
                <p>Administração de contas reais</p>
              </div>
            </div>
            <label>
              Username
              <input value={newUser.username} onChange={event => setNewUser(current => ({ ...current, username: event.target.value }))} />
            </label>
            <label>
              Nome completo
              <input value={newUser.fullName} onChange={event => setNewUser(current => ({ ...current, fullName: event.target.value }))} />
            </label>
            <label>
              Role
              <select value={newUser.role} onChange={event => setNewUser(current => ({ ...current, role: event.target.value }))}>
                <option value="Operator">Operator</option>
                <option value="Supervisor">Supervisor</option>
                <option value="Admin">Admin</option>
              </select>
            </label>
            <label>
              Password
              <input type="password" value={newUser.password ?? ''} onChange={event => setNewUser(current => ({ ...current, password: event.target.value }))} />
            </label>
            <label className="switch-row">
              <input type="checkbox" checked={newUser.isActive} onChange={event => setNewUser(current => ({ ...current, isActive: event.target.checked }))} />
              <span>Utilizador ativo</span>
            </label>
            <button type="button" onClick={() => void handleCreateUser()}>Criar utilizador</button>
          </article>
        ) : null}
      </div>

      {canManageUsers ? (
        <div className="profiles-list">
          {userDrafts.map(user => (
            <article key={user.username} className="profile-user-card">
              <div className="profile-user-card__head">
                <div>
                  <strong>{user.fullName}</strong>
                  <p>{user.username}</p>
                </div>
                <span>{user.role}</span>
              </div>
              <div className="profile-user-card__fields">
                <input value={user.fullName} onChange={event => setUserDrafts(current => current.map(item => item.username === user.username ? { ...item, fullName: event.target.value } : item))} />
                <select value={user.role} onChange={event => setUserDrafts(current => current.map(item => item.username === user.username ? { ...item, role: event.target.value } : item))}>
                  <option value="Operator">Operator</option>
                  <option value="Supervisor">Supervisor</option>
                  <option value="Admin">Admin</option>
                </select>
                <label className="switch-row">
                  <input type="checkbox" checked={user.isActive} onChange={event => setUserDrafts(current => current.map(item => item.username === user.username ? { ...item, isActive: event.target.checked } : item))} />
                  <span>Ativo</span>
                </label>
              </div>
              <div className="profile-user-card__meta">
                <small>Último login: {user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : 'nunca'}</small>
                <button type="button" onClick={() => void handleSaveUser(user)}>Guardar</button>
              </div>
            </article>
          ))}
        </div>
      ) : null}

      {message ? <div className="profiles-panel__message">{message}</div> : null}
    </section>
  );
}
