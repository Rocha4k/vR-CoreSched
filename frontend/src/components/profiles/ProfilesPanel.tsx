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

const roles = ['Operator', 'Supervisor', 'Admin'];

const emptyUser: UpsertUserRequest = { username: '', fullName: '', role: 'Operator', isActive: true, password: '' };

export function ProfilesPanel({ currentUser, users, canManageUsers, onUpdateProfile, onCreateUser, onSaveUser }: Props) {
  const [profileDraft, setProfileDraft] = useState<UpdateProfileRequest>({ fullName: currentUser.fullName, currentPassword: '', newPassword: '' });
  const [userDrafts, setUserDrafts] = useState<UserProfile[]>(users);
  const [newUser, setNewUser] = useState<UpsertUserRequest>(emptyUser);
  const [message, setMessage] = useState<{ text: string; error?: boolean } | null>(null);

  useEffect(() => {
    setProfileDraft({ fullName: currentUser.fullName, currentPassword: '', newPassword: '' });
  }, [currentUser.fullName]);

  useEffect(() => {
    setUserDrafts(users);
  }, [users]);

  const handleSaveProfile = async () => {
    try {
      const result = await onUpdateProfile(profileDraft);
      setProfileDraft({ fullName: result.fullName, currentPassword: '', newPassword: '' });
      setMessage({ text: 'Profile updated.' });
    } catch {
      setMessage({ text: 'Could not update the profile. Check your current password.', error: true });
    }
  };

  const handleSaveUser = async (user: UserProfile) => {
    try {
      const result = await onSaveUser({
        username: user.username,
        fullName: user.fullName,
        role: user.role,
        isActive: user.isActive,
        password: null
      });

      setUserDrafts(current => current.map(item => item.username === result.username ? result : item));
      setMessage({ text: `User "${result.username}" updated.` });
    } catch {
      setMessage({ text: `Could not update "${user.username}".`, error: true });
    }
  };

  const handleCreateUser = async () => {
    if (!newUser.username.trim() || !newUser.fullName.trim()) {
      setMessage({ text: 'Username and full name are required.', error: true });
      return;
    }

    try {
      const result = await onCreateUser(newUser);
      setUserDrafts(current => [...current.filter(item => item.username !== result.username), result].sort((left, right) => left.fullName.localeCompare(right.fullName)));
      setNewUser(emptyUser);
      setMessage({ text: `User "${result.username}" created.` });
    } catch {
      setMessage({ text: 'Could not create the user.', error: true });
    }
  };

  return (
    <div className="profiles-panel">
      <div className="page-head">
        <div>
          <p className="eyebrow">Account</p>
          <h1>Profiles</h1>
          <p>Update your own details, and manage operator, supervisor and admin accounts.</p>
        </div>
        {message ? <span className={message.error ? 'toast toast--error' : 'toast'}>{message.text}</span> : null}
      </div>

      <section className="panel">
        <div className="panel-header">
          <h2>{canManageUsers ? 'Your profile & new accounts' : 'Your profile'}</h2>
          <span className="badge">{currentUser.role}</span>
        </div>

        <div className="profiles-grid">
          <article className="profile-card">
            <div className="profile-card__head">
              <div>
                <strong>{currentUser.fullName}</strong>
                <p>{currentUser.username}</p>
              </div>
              <span className={currentUser.isActive ? 'badge badge--ok' : 'badge badge--muted'}>
                {currentUser.isActive ? 'Active' : 'Inactive'}
              </span>
            </div>

            <label className="field">
              <span className="field__label">Full name</span>
              <input value={profileDraft.fullName} onChange={event => setProfileDraft(current => ({ ...current, fullName: event.target.value }))} />
            </label>
            <label className="field">
              <span className="field__label">Current password</span>
              <input type="password" autoComplete="current-password" value={profileDraft.currentPassword ?? ''} onChange={event => setProfileDraft(current => ({ ...current, currentPassword: event.target.value }))} />
            </label>
            <label className="field">
              <span className="field__label">New password</span>
              <input type="password" autoComplete="new-password" value={profileDraft.newPassword ?? ''} onChange={event => setProfileDraft(current => ({ ...current, newPassword: event.target.value }))} />
            </label>
            <p className="inline-note">Leave both password fields empty to change only the name.</p>
            <button type="button" className="btn btn--primary" onClick={() => void handleSaveProfile()}>Save profile</button>
          </article>

          {canManageUsers ? (
            <article className="profile-card">
              <div className="profile-card__head">
                <div>
                  <strong>New user</strong>
                  <p>Create a real account</p>
                </div>
              </div>

              <label className="field">
                <span className="field__label">Username</span>
                <input value={newUser.username} autoComplete="off" onChange={event => setNewUser(current => ({ ...current, username: event.target.value }))} />
              </label>
              <label className="field">
                <span className="field__label">Full name</span>
                <input value={newUser.fullName} onChange={event => setNewUser(current => ({ ...current, fullName: event.target.value }))} />
              </label>
              <label className="field">
                <span className="field__label">Role</span>
                <select value={newUser.role} onChange={event => setNewUser(current => ({ ...current, role: event.target.value }))}>
                  {roles.map(role => <option key={role} value={role}>{role}</option>)}
                </select>
              </label>
              <label className="field">
                <span className="field__label">Password</span>
                <input type="password" autoComplete="new-password" value={newUser.password ?? ''} onChange={event => setNewUser(current => ({ ...current, password: event.target.value }))} />
              </label>
              <label className="switch-row">
                <input type="checkbox" checked={newUser.isActive} onChange={event => setNewUser(current => ({ ...current, isActive: event.target.checked }))} />
                <span>Account active</span>
              </label>
              <button type="button" className="btn" onClick={() => void handleCreateUser()}>Create user</button>
            </article>
          ) : null}
        </div>
      </section>

      {canManageUsers ? (
        <section className="panel">
          <div className="panel-header">
            <h2>All users</h2>
            <span>{userDrafts.length} accounts</span>
          </div>

          {userDrafts.length === 0 ? (
            <div className="empty-state">No users loaded.</div>
          ) : (
            <div className="profiles-list">
              {userDrafts.map(user => (
                <article key={user.username} className="profile-user-card">
                  <div className="profile-user-card__head">
                    <div>
                      <strong>{user.fullName}</strong>
                      <p>{user.username}</p>
                    </div>
                    <span className="badge">{user.role}</span>
                  </div>

                  <div className="profile-user-card__fields">
                    <label className="field">
                      <span className="field__label">Full name</span>
                      <input
                        value={user.fullName}
                        onChange={event => setUserDrafts(current => current.map(item => item.username === user.username ? { ...item, fullName: event.target.value } : item))}
                      />
                    </label>
                    <label className="field">
                      <span className="field__label">Role</span>
                      <select
                        value={user.role}
                        onChange={event => setUserDrafts(current => current.map(item => item.username === user.username ? { ...item, role: event.target.value } : item))}
                      >
                        {roles.map(role => <option key={role} value={role}>{role}</option>)}
                      </select>
                    </label>
                    <label className="switch-row">
                      <input
                        type="checkbox"
                        checked={user.isActive}
                        onChange={event => setUserDrafts(current => current.map(item => item.username === user.username ? { ...item, isActive: event.target.checked } : item))}
                      />
                      <span>Active</span>
                    </label>
                  </div>

                  <div className="profile-user-card__meta">
                    <span>Last sign-in: {user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : 'never'}</span>
                    <button type="button" className="btn btn--sm" onClick={() => void handleSaveUser(user)}>Save</button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      ) : null}
    </div>
  );
}
