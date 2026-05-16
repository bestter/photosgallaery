import React, { useState, useEffect, useMemo, useDeferredValue } from "react";
import {
  getUserRole,
  isTokenExpired,
  getUsernameFromToken,
} from "../authHelper";
import api from "../api";
import { useTranslation } from "react-i18next";
import AdminLayout from "../components/AdminLayout";
import toast from "react-hot-toast";

export default function Dashboard() {
  const { t } = useTranslation();
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const deferredSearchTerm = useDeferredValue(searchTerm);
  const currentUsername = getUsernameFromToken(localStorage.getItem("token"));

  // ⚡ Bolt: Use useDeferredValue and useMemo to prevent expensive filtering from blocking the main UI thread during typing
  const filteredUsers = useMemo(() => {
    return users.filter((u) => {
      if (!deferredSearchTerm) return true;
      const term = deferredSearchTerm.toLowerCase();
      const username = u.username || u.Username || "";
      const email = u.email || u.Email || "";
      return username.toLowerCase().includes(term) || email.toLowerCase().includes(term);
    });
  }, [users, deferredSearchTerm]);

  // Vérification de la session et du rôle via le token
  useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token || isTokenExpired(token) || getUserRole(token) !== "Admin") {
      window.location.href = "/";
      return;
    }

    const fetchUsers = async () => {
      try {
        const response = await api.get("/admin/users");
        setUsers(response.data);
      } catch (error) {
        console.error("Error fetching users:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchUsers();
  }, []);

  const handleRoleUpdate = async (userId, newRole) => {
    try {
      await api.put(`/admin/users/${userId}/role`, { role: newRole });
      setUsers((prev) =>
        prev.map((u) =>
          u.id === userId || u.Id === userId
            ? { ...u, role: newRole, Role: newRole }
            : u,
        ),
      );
    } catch (error) {
      console.error(`Error updating role to ${newRole}:`, error);
      toast.error(t("admin.dashboard.role_update_error"));
    }
  };

  const topActions = (
    <div className="flex items-center gap-4 flex-1 max-w-xl mr-auto">
      <div className="relative w-full">
        <span
          className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant text-xl"
          aria-hidden="true"
        >
          search
        </span>
        <input
          className="w-full pl-11 pr-4 py-2 bg-surface-container-low border border-outline-variant/30 rounded-xl focus:ring-2 focus:ring-primary text-sm transition-all text-on-surface placeholder:text-on-surface-variant"
          placeholder={t("admin.dashboard.search_placeholder")}
          type="text"
          aria-label={t("admin.dashboard.search_placeholder")}
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />
      </div>
    </div>
  );

  return (
    <AdminLayout
      title={t("admin.dashboard.title")}
      subtitle={t("admin.dashboard.subtitle_extended")}
      topActions={topActions}
    >
      <div className="space-y-8">
        {/* Stats Summary */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="bg-surface-container-low p-6 rounded-2xl border border-outline-variant/30 shadow-md">
            <div className="flex items-center justify-between mb-4">
              <span
                className="p-2 bg-primary/10 text-primary rounded-lg material-symbols-outlined"
                aria-hidden="true"
              >
                person
              </span>
            </div>
            <p className="text-on-surface-variant text-sm font-medium">
              {t("admin.dashboard.total_users")}
            </p>
            <h3 className="text-3xl font-bold mt-1 text-on-surface">
              {loading ? "-" : users.length}
            </h3>
          </div>
          <div className="bg-surface-container-low p-6 rounded-2xl border border-outline-variant/30 shadow-md">
            <div className="flex items-center justify-between mb-4">
              <span
                className="p-2 bg-primary/10 text-primary rounded-lg material-symbols-outlined"
                aria-hidden="true"
              >
                camera
              </span>
            </div>
            <p className="text-on-surface-variant text-sm font-medium">
              {t("admin.dashboard.active_creators")}
            </p>
            <h3 className="text-3xl font-bold mt-1 text-on-surface">
              {loading
                ? "-"
                : users.filter(
                    (u) => u.role === "Creator" || u.Role === "Creator",
                  ).length}
            </h3>
          </div>
        </div>
        {/* Users Table */}
        <div className="bg-surface-container-low rounded-2xl border border-outline-variant/30 overflow-hidden shadow-md">
          <div className="px-6 py-4 border-b border-outline-variant/30 flex items-center justify-between">
            <h4 className="font-bold text-lg text-on-surface">
              {t("admin.dashboard.user_list")}
            </h4>
            <div className="flex gap-2">
              <button className="flex items-center gap-2 px-4 py-2 bg-surface-container-high hover:bg-surface-container-highest text-sm font-medium rounded-lg transition-colors text-on-surface">
                <span
                  className="material-symbols-outlined text-sm"
                  aria-hidden="true"
                >
                  filter_list
                </span>{" "}
                {t("admin.dashboard.filter")}
              </button>
              <button className="flex items-center gap-2 px-4 py-2 bg-primary text-background-dark hover:bg-primary/90 text-sm font-bold rounded-lg transition-colors shadow-[0_0_15px_rgba(0,206,209,0.3)]">
                <span
                  className="material-symbols-outlined text-sm"
                  aria-hidden="true"
                >
                  person_add
                </span>{" "}
                {t("admin.dashboard.new_user")}
              </button>
            </div>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead className="bg-surface-container/50 border-b border-outline-variant/40">
                <tr>
                  <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">
                    {t("admin.dashboard.table.username")}
                  </th>
                  <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">
                    {t("admin.dashboard.table.email")}
                  </th>
                  <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">
                    {t("admin.dashboard.table.groups")}
                  </th>
                  <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">
                    {t("admin.dashboard.table.role")}
                  </th>
                  <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider text-right">
                    {t("admin.dashboard.table.actions")}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-outline-variant/20 text-sm">
                {loading ? (
                  <tr>
                    <td colSpan="5" className="text-center py-4 text-slate-500">
                      Chargement...
                    </td>
                  </tr>
                ) : (
                  filteredUsers.map((user) => {
                    const userId = user.id || user.Id;
                    const username = user.username || user.Username;
                    const email = user.email || user.Email;
                    const role = user.role || user.Role;
                    const groups = user.groups || user.Groups || [];
                    const isCurrentUser = username === currentUsername;

                    return (
                      <tr
                        key={userId}
                        className="hover:bg-surface-container-high/50 group transition-colors"
                      >
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="flex items-center gap-3">
                            <div className="w-8 h-8 rounded-full bg-surface-container-highest overflow-hidden flex items-center justify-center font-bold text-on-surface-variant">
                              {username
                                ? username.substring(0, 2).toUpperCase()
                                : "?"}
                            </div>
                            <span className="font-semibold text-on-surface group-hover:text-primary transition-colors">
                              {username}
                            </span>
                          </div>
                        </td>
                        <td className="px-6 py-4 text-on-surface-variant">
                          {email}
                        </td>
                        <td className="px-6 py-4">
                          {groups && groups.length > 0 ? (
                            groups.length > 2 ? (
                              <div className="group relative inline-block cursor-pointer">
                                <span className="px-2.5 py-1 text-xs font-medium bg-surface-container-high text-on-surface-variant rounded-full border border-outline-variant/30 hover:bg-surface-container-highest transition-colors">
                                  Groupes ({groups.length})
                                </span>
                                <div className="absolute left-1/2 -translate-x-1/2 bottom-full mb-2 hidden group-hover:block w-max bg-surface-container-highest text-on-surface text-xs rounded-lg py-2 px-3 shadow-lg z-10 before:content-[''] before:absolute before:-bottom-1 before:left-1/2 before:-translate-x-1/2 before:border-4 before:border-transparent before:border-t-surface-container-highest">
                                  <div className="flex flex-col gap-1 items-center">
                                    {groups.map((g, i) => (
                                      <span
                                        key={i}
                                        className="whitespace-nowrap font-medium"
                                      >
                                        {g}
                                      </span>
                                    ))}
                                  </div>
                                </div>
                              </div>
                            ) : (
                              <div className="flex flex-wrap gap-1">
                                {groups.map((g, i) => (
                                  <span
                                    key={i}
                                    className="px-2.5 py-1 text-xs font-medium bg-surface-container-high text-on-surface-variant rounded-full border border-outline-variant/30"
                                  >
                                    {g}
                                  </span>
                                ))}
                              </div>
                            )
                          ) : (
                            <span className="text-xs text-on-surface-variant/70 italic">
                              {t("admin.dashboard.table.none")}
                            </span>
                          )}
                        </td>
                        <td className="px-6 py-4">
                          {role === "Admin" && (
                            <span className="px-2.5 py-1 text-xs font-bold bg-tertiary/20 text-tertiary rounded-full border border-tertiary/30">
                              {t("admin.dashboard.role.admin")}
                            </span>
                          )}
                          {role === "Creator" && (
                            <span className="px-2.5 py-1 text-xs font-medium bg-primary/20 text-primary rounded-full border border-primary/30">
                              {t("admin.dashboard.role.creator")}
                            </span>
                          )}
                          {(role === "User" ||
                            (!role && role !== "Forbidden")) && (
                            <span className="px-2.5 py-1 text-xs font-medium bg-surface-container-high text-on-surface-variant rounded-full border border-outline-variant/30">
                              {t("admin.dashboard.role.user")}
                            </span>
                          )}
                          {role === "Forbidden" && (
                            <span className="px-2.5 py-1 text-xs font-bold bg-error/20 text-error rounded-full border border-error/30">
                              {t("admin.dashboard.role.banned")}
                            </span>
                          )}
                        </td>
                        <td className="px-6 py-4 text-right">
                          <div className="flex items-center justify-end gap-2">
                            {role !== "Admin" &&
                              (role === "Creator" ? (
                                <button
                                  onClick={() =>
                                    handleRoleUpdate(userId, "User")
                                  }
                                  className="px-3 py-1.5 text-xs font-bold text-secondary border border-secondary/50 hover:bg-secondary/10 rounded-lg transition-colors"
                                >
                                  {t("admin.dashboard.action.demote_creator")}
                                </button>
                              ) : (
                                <button
                                  onClick={() =>
                                    handleRoleUpdate(userId, "Creator")
                                  }
                                  className="px-3 py-1.5 text-xs font-bold text-primary border border-primary/50 hover:bg-primary/10 rounded-lg transition-colors"
                                >
                                  {t("admin.dashboard.action.promote_creator")}
                                </button>
                              ))}
                            {role === "Admin" ? (
                              isCurrentUser ? (
                                <button
                                  disabled
                                  className="px-3 py-1.5 text-xs font-bold text-on-surface-variant/50 border border-outline-variant/30 cursor-not-allowed rounded-lg bg-surface-container/50"
                                  title={t("admin.dashboard.action.self_action_tooltip")}
                                >
                                  {t("admin.dashboard.action.current_admin")}
                                </button>
                              ) : (
                                <button
                                  onClick={() =>
                                    handleRoleUpdate(userId, "User")
                                  }
                                  className="px-3 py-1.5 text-xs font-bold text-secondary border border-secondary/50 hover:bg-secondary/10 rounded-lg transition-colors"
                                >
                                  {t("admin.dashboard.action.demote_admin")}
                                </button>
                              )
                            ) : (
                              <button
                                onClick={() =>
                                  handleRoleUpdate(userId, "Admin")
                                }
                                className="px-3 py-1.5 text-xs font-bold bg-primary text-on-primary hover:bg-primary/90 rounded-lg transition-colors"
                              >
                                {t("admin.dashboard.action.promote_admin")}
                              </button>
                            )}
                            <button
                              disabled={isCurrentUser}
                              onClick={() => {
                                if (!isCurrentUser) {
                                  handleRoleUpdate(
                                    userId,
                                    role === "Forbidden" ? "User" : "Forbidden",
                                  );
                                }
                              }}
                              className={`p-1.5 rounded-lg transition-colors ${isCurrentUser ? "text-on-surface-variant/50 cursor-not-allowed bg-surface-container/50" : role === "Forbidden" ? "text-tertiary hover:bg-tertiary/10" : "text-error hover:bg-error/10"}`}
                              title={
                                isCurrentUser
                                  ? t("admin.dashboard.action.self_action_tooltip")
                                  : role === "Forbidden"
                                    ? t("admin.dashboard.action.unban")
                                    : t("admin.dashboard.action.ban")
                              }
                              aria-label={
                                isCurrentUser
                                  ? t("admin.dashboard.action.self_action_tooltip")
                                  : role === "Forbidden"
                                    ? t("admin.dashboard.action.unban")
                                    : t("admin.dashboard.action.ban")
                              }
                            >
                              <span
                                className="material-symbols-outlined text-lg leading-none"
                                aria-hidden="true"
                              >
                                {role === "Forbidden" ? "lock_open" : "block"}
                              </span>
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
          <div className="px-6 py-4 bg-surface-container/30 border-t border-outline-variant/30 flex items-center justify-between">
            <p className="text-xs text-on-surface-variant font-medium tracking-wide">
              {t("admin.dashboard.showing_users", { count: filteredUsers.length })}
            </p>
            <div className="flex gap-1">
              <button
                className="w-8 h-8 flex items-center justify-center rounded bg-surface-container hover:bg-surface-container-high border border-outline-variant/30 text-on-surface-variant hover:text-primary transition-colors"
                aria-label="Page précédente"
              >
                <span
                  className="material-symbols-outlined text-sm"
                  aria-hidden="true"
                >
                  chevron_left
                </span>
              </button>
              <button
                className="w-8 h-8 flex items-center justify-center rounded bg-primary text-background-dark font-bold text-xs"
                aria-current="page"
                aria-label="Page 1"
              >
                1
              </button>
              <button
                className="w-8 h-8 flex items-center justify-center rounded bg-surface-container hover:bg-surface-container-high border border-outline-variant/30 text-on-surface-variant hover:text-primary transition-colors text-xs font-bold"
                aria-label="Aller à la page 2"
              >
                2
              </button>
              <button
                className="w-8 h-8 flex items-center justify-center rounded bg-surface-container hover:bg-surface-container-high border border-outline-variant/30 text-on-surface-variant hover:text-primary transition-colors text-xs font-bold"
                aria-label="Aller à la page 3"
              >
                3
              </button>
              <button
                className="w-8 h-8 flex items-center justify-center rounded bg-surface-container hover:bg-surface-container-high border border-outline-variant/30 text-on-surface-variant hover:text-primary transition-colors"
                aria-label="Page suivante"
              >
                <span
                  className="material-symbols-outlined text-sm"
                  aria-hidden="true"
                >
                  chevron_right
                </span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
}
