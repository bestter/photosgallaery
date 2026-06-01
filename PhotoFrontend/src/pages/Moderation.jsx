import React, { useState, useEffect, useMemo, useDeferredValue } from "react";
import { useTranslation } from "react-i18next";
import { getUserRole, isTokenExpired } from "../authHelper";
import api from "../api";
import AdminLayout from "../components/AdminLayout";

export default function Moderation() {
  const { t } = useTranslation();
  const [reports, setReports] = useState([]);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [loading, setLoading] = useState(true);
  const [stats, setStats] = useState({ total: 0, pending: 0, processed: 0 });
    const [searchTerm, setSearchTerm] = useState("");
  const deferredSearchTerm = useDeferredValue(searchTerm);

  // Vérification de la session et du rôle via le token
  useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token || isTokenExpired(token) || getUserRole(token) !== "Admin") {
      window.location.href = "/";
      return;
    }

    const fetchReports = async (currentPage = 1, append = false) => {
      try {
        setLoading(true);
        const params = new URLSearchParams({
          page: currentPage,
          pageSize: 20,
        });
        if (deferredSearchTerm) params.append("search", deferredSearchTerm);

        const response = await api.get(`/admin/reports?${params.toString()}`);
        setReports((prev) =>
          append ? [...prev, ...response.data] : response.data,
        );
        setHasMore(response.data.length === 20);
      } catch (error) {
        console.error(
          "Erreur lors de la récupération des signalements:",
          error,
        );
      } finally {
        setLoading(false);
      }
    };

    // eslint-disable-next-line react-hooks/set-state-in-effect
    setPage(1);
    fetchReports(1, false);
    const fetchStats = async () => {
      try {
        const response = await api.get("/admin/reports/stats");
        setStats(response.data);
      } catch (error) {
        console.error("Erreur lors de la récupération des stats:", error);
      }
    };
    fetchStats();
  }, [deferredSearchTerm]);





  // ⚡ Bolt: Server-side pagination and search replaces most client-side filtering.
  const filteredReportsList = useMemo(() => {
    return reports.filter(
      (r) =>
        r.status === "Pending" ||
        r.Status === "Pending" ||
        (!r.status && !r.Status),
    );
  }, [reports]);

  const handleDismiss = async (reportId) => {
    try {
      await api.delete(`/admin/reports/${reportId}`);
      setReports((prev) =>
        prev.map((r) =>
          r.reportId === reportId || r.ReportId === reportId
            ? { ...r, status: "Processed", Status: "Processed" }
            : r,
        ),
      );
    } catch (error) {
      console.error("Erreur lors de l'annulation du signalement:", error);
    }
  };

  const getImageUrl = (url) => {
    if (!url) return "";
    if (url.startsWith("http")) return url;
    const backendRoot = api.defaults.baseURL.replace(/\/api$/, "");
    return backendRoot + url;
  };

  const topActions = (
    <div className="flex items-center gap-4 flex-1 max-w-xl mr-auto"></div>
  );

  return (
    <AdminLayout
      title={t("admin.moderation.title")}
      subtitle={t("admin.moderation.subtitle")}
      topActions={topActions}
    >
      <div className="space-y-8">
        {/* Stats Grid */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="bg-surface-container-low p-6 rounded-xl border border-outline-variant/30 shadow-md">
            <div className="flex justify-between items-start">
              <div>
                <p className="text-sm font-medium text-on-surface-variant">
                  {t("admin.moderation.total_reports")}
                </p>
                <p className="text-3xl font-bold mt-1 text-on-surface">
                  {stats.total}
                </p>
              </div>
              <div className="p-2 bg-primary/10 rounded-lg">
                <span
                  aria-hidden="true"
                  className="material-symbols-outlined text-primary"
                >
                  flag
                </span>
              </div>
            </div>
            <div className="mt-4 flex items-center gap-1">
              <span className="text-emerald-500 text-sm font-semibold">
                +12%
              </span>
              <span className="text-xs text-slate-400 italic">
                {t("admin.moderation.since_last_month")}
              </span>
            </div>
          </div>
          <div className="bg-surface-container-low p-6 rounded-xl border border-outline-variant/30 shadow-md">
            <div className="flex justify-between items-start">
              <div>
                <p className="text-sm font-medium text-on-surface-variant">
                  {t("admin.moderation.pending")}
                </p>
                <p className="text-3xl font-bold mt-1 text-primary">
                  {stats.pending}
                </p>
              </div>
              <div className="p-2 bg-amber-500/10 rounded-lg">
                <span
                  aria-hidden="true"
                  className="material-symbols-outlined text-amber-500"
                >
                  pending_actions
                </span>
              </div>
            </div>
            <div className="mt-4 flex items-center gap-1">
              <span className="text-rose-500 text-sm font-semibold">-5%</span>
              <span className="text-xs text-on-surface-variant italic">
                {t("admin.moderation.backlog_reduction")}
              </span>
            </div>
          </div>
          <div className="bg-surface-container-low p-6 rounded-xl border border-outline-variant/30 shadow-md">
            <div className="flex justify-between items-start">
              <div>
                <p className="text-sm font-medium text-on-surface-variant">
                  {t("admin.moderation.processed")}
                </p>
                <p className="text-3xl font-bold mt-1 text-on-surface">
                  {stats.processed}
                </p>
              </div>
              <div className="p-2 bg-emerald-500/10 rounded-lg">
                <span
                  aria-hidden="true"
                  className="material-symbols-outlined text-emerald-500"
                >
                  check_circle
                </span>
              </div>
            </div>
            <div className="mt-4 flex items-center gap-1">
              <span className="text-emerald-500 text-sm font-semibold">
                +8%
              </span>
              <span className="text-xs text-on-surface-variant italic">
                {t("admin.moderation.efficiency_increase")}
              </span>
            </div>
          </div>
        </div>
        {/* Flagged Images Table Section */}
        <div className="bg-surface-container-low rounded-xl border border-outline-variant/30 shadow-md overflow-hidden">
          <div className="p-6 border-b border-outline-variant/30 flex flex-wrap justify-between items-center gap-4">
            <h3 className="text-lg font-bold text-on-surface">
              {t("admin.moderation.reported_images")}
            </h3>
            <div className="flex gap-3">
              <div className="relative">
                <span
                  aria-hidden="true"
                  className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant text-sm"
                >
                  search
                </span>
                <input
                  className="pl-10 pr-10 py-2 bg-surface-container border border-outline-variant/30 rounded-lg text-sm focus:ring-2 focus:ring-primary w-64 text-on-surface"
                  placeholder={t("admin.moderation.search_placeholder")}
                  type="text"
                  aria-label={t("admin.moderation.search_placeholder")}
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
                {searchTerm && (
                  <button
                    type="button"
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-on-surface-variant hover:text-primary transition-colors flex items-center justify-center focus:outline-none focus-visible:ring-2 focus-visible:ring-primary rounded-full"
                    onClick={() => setSearchTerm("")}
                    aria-label={t("admin.moderation.clear_search")}
                    title={t("admin.moderation.clear_search")}
                  >
                    <span
                      className="material-symbols-outlined text-[18px]"
                      aria-hidden="true"
                    >
                      close
                    </span>
                  </button>
                )}
              </div>
              <button className="flex items-center gap-2 px-4 py-2 bg-primary/10 text-primary rounded-lg text-sm font-medium hover:bg-primary/20 transition-colors">
                <span
                  aria-hidden="true"
                  className="material-symbols-outlined text-sm"
                >
                  filter_list
                </span>{" "}
                {t("admin.moderation.filter")}
              </button>
            </div>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-surface-container/50 border-b border-outline-variant/40">
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-wider text-on-surface-variant">
                    {t("admin.moderation.table.image")}
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-wider text-on-surface-variant">
                    {t("admin.moderation.table.image_name")}
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-wider text-on-surface-variant">
                    {t("admin.moderation.table.reported_user")}
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-wider text-on-surface-variant">
                    {t("admin.moderation.table.reason")}
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-wider text-on-surface-variant text-right">
                    {t("admin.moderation.table.actions")}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-outline-variant/20 text-sm">
                {filteredReportsList.map((report) => {
                  const rId = report.reportId || report.ReportId;
                  const pUrl = report.photoUrl || report.PhotoUrl;
                  const uploader = report.uploader || report.Uploader;
                  const reason = report.reason || report.Reason;
                  const photoId = report.photoId || report.PhotoId;

                  return (
                    <tr
                      key={rId}
                      className="hover:bg-surface-container-high/50 group transition-colors"
                    >
                      <td className="px-6 py-4">
                        <div className="w-12 h-12 rounded bg-surface-container-highest overflow-hidden border border-outline-variant">
                          <img
                            className="w-full h-full object-cover"
                            alt={t("admin.moderation.table.image")}
                            src={getImageUrl(pUrl)}
                          />
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <a
                          className="text-primary font-medium hover:underline text-sm"
                          href="#"
                          onClick={(e) => e.preventDefault()}
                        >
                          {t("admin.moderation.photo_num", { id: photoId })}
                        </a>
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-2">
                          <div className="w-6 h-6 rounded-full bg-primary/20 flex items-center justify-center text-[10px] font-bold text-primary">
                            {uploader
                              ? uploader.substring(0, 2).toUpperCase()
                              : "??"}
                          </div>
                          <span className="text-primary font-medium text-sm">
                            {uploader || t("admin.moderation.anonymous")}
                          </span>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-error/10 text-error border border-error/20">
                          {reason}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-right space-x-2">
                        <button
                          className="px-3 py-1.5 bg-error hover:bg-error/90 text-on-error text-xs font-bold rounded shadow-sm transition-all uppercase tracking-wide"
                          aria-label={t("admin.moderation.action.delete")}
                        >
                          {t("admin.moderation.action.delete")}
                        </button>
                        <button
                          onClick={() => handleDismiss(rId)}
                          className="px-3 py-1.5 bg-surface-container-high hover:bg-surface-container-highest text-on-surface text-xs font-bold rounded shadow-sm transition-all uppercase tracking-wide border border-outline-variant/30"
                          aria-label={t("admin.moderation.action.dismiss")}
                        >
                          {t("admin.moderation.action.dismiss")}
                        </button>
                      </td>
                    </tr>
                  );
                })}
                {filteredReportsList.length === 0 && (
                  <tr>
                    <td
                      colSpan="5"
                      className="px-6 py-4 text-center text-slate-500"
                    >
                      {t("admin.moderation.no_reports")}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          {/* Pagination */}
          {hasMore && !loading && filteredReportsList.length > 0 && (
            <div className="p-6 border-t border-outline-variant/30 flex items-center justify-center bg-surface-container/30">
              <button
                onClick={() => {
                  const nextPage = page + 1;
                  setPage(nextPage);
                  setLoading(true);
                  const params = new URLSearchParams({
                    page: nextPage,
                    pageSize: 20,
                  });
                  if (deferredSearchTerm)
                    params.append("search", deferredSearchTerm);

                  api
                    .get(`/admin/reports?${params.toString()}`)
                    .then((response) => {
                      setReports((prev) => [...prev, ...response.data]);
                      setHasMore(response.data.length === 20);
                    })
                    .finally(() => setLoading(false));
                }}
                className="px-6 py-2 bg-primary text-background-dark font-bold rounded-lg text-sm hover:brightness-110 transition-all"
              >
                {t("admin.moderation.load_more")}
              </button>
            </div>
          )}
        </div>
      </div>
    </AdminLayout>
  );
}
