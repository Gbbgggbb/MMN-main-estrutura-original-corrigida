(function (window) {
    const statusLabels = {
        1: "Prospecto",
        2: "Cliente",
        3: "Cliente preferencial",
        4: "Consultor de bem-estar"
    };

    const temperatureLabels = {
        1: "Frio",
        2: "Morno",
        3: "Quente"
    };

    const defaultChecklist = [
        { title: "Conversar com o cliente", isCompleted: false },
        { title: "Ligar para o cliente", isCompleted: false },
        { title: "Apresentar produto", isCompleted: false }
    ];

    let hasUnsavedDraft = false;

    function getCurrentUserId() {
        return document.body.dataset.userId || "anonymous";
    }

    function getStorageKey() {
        return `mmn.prospects.${getCurrentUserId()}`;
    }

    function getFlashKey() {
        return `mmn.flash.${getCurrentUserId()}`;
    }

    function getSettingsKey() {
        return `mmn.settings.${getCurrentUserId()}`;
    }

    function loadSettings() {
        try {
            return JSON.parse(localStorage.getItem(getSettingsKey()) || '{}');
        } catch {
            return {};
        }
    }

    function isUnsavedAlertEnabled() {
        return Boolean(loadSettings().warnUnsaved);
    }

    function markDraftChanged() {
        hasUnsavedDraft = true;
    }

    function markDraftSaved() {
        hasUnsavedDraft = false;
    }

    function normalizeChecklist(items) {
        const normalized = (Array.isArray(items) ? items : [])
            .map(function (item) {
                return {
                    title: String(item?.title || item?.Title || "").trim(),
                    isCompleted: Boolean(item?.isCompleted || item?.IsCompleted)
                };
            })
            .filter(function (item) {
                return item.title;
            })
            .slice(0, 10);

        return normalized.length > 0 ? normalized : defaultChecklist.map(function (item) { return { ...item }; });
    }

    function normalizeProspect(prospect) {
        const nextContactDate = String(prospect?.nextContactDate || prospect?.NextContactDate || "").slice(0, 10);
        const createdAt = String(prospect?.createdAt || prospect?.CreatedAt || new Date().toISOString());

        return {
            id: String(prospect?.id || prospect?.Id || crypto.randomUUID()),
            fullName: String(prospect?.fullName || prospect?.FullName || "").trim(),
            phone: String(prospect?.phone || prospect?.Phone || "").trim(),
            email: String(prospect?.email || prospect?.Email || "").trim(),
            city: String(prospect?.city || prospect?.City || "").trim(),
            source: String(prospect?.source || prospect?.Source || "").trim(),
            temperature: Number(prospect?.temperature || prospect?.Temperature || 2),
            status: Number(prospect?.status || prospect?.Status || 1),
            nextContactDate: nextContactDate || new Date().toISOString().slice(0, 10),
            notes: String(prospect?.notes || prospect?.Notes || "").trim(),
            checklistItems: normalizeChecklist(prospect?.checklistItems || prospect?.ChecklistItems),
            createdAt: createdAt
        };
    }

    function loadProspects() {
        try {
            const raw = localStorage.getItem(getStorageKey());
            if (!raw) {
                return [];
            }

            const parsed = JSON.parse(raw);
            if (!Array.isArray(parsed)) {
                return [];
            }

            return parsed.map(normalizeProspect);
        } catch {
            return [];
        }
    }

    function saveProspects(prospects) {
        const normalized = prospects.map(normalizeProspect);
        localStorage.setItem(getStorageKey(), JSON.stringify(normalized));
        return normalized;
    }

    function getProspectById(id) {
        return loadProspects().find(function (prospect) {
            return prospect.id === id;
        }) || null;
    }

    function upsertProspect(prospect) {
        const normalized = normalizeProspect(prospect);
        const prospects = loadProspects();
        const index = prospects.findIndex(function (item) {
            return item.id === normalized.id;
        });

        if (index >= 0) {
            normalized.createdAt = prospects[index].createdAt;
            prospects[index] = normalized;
        } else {
            prospects.push(normalized);
        }

        return saveProspects(prospects);
    }

    function updateChecklist(id, checklistItems) {
        const prospects = loadProspects();
        const prospect = prospects.find(function (item) {
            return item.id === id;
        });

        if (!prospect) {
            return false;
        }

        prospect.checklistItems = normalizeChecklist(checklistItems);
        saveProspects(prospects);
        return true;
    }

    function deleteProspect(id) {
        const prospects = loadProspects().filter(function (prospect) {
            return prospect.id !== id;
        });

        saveProspects(prospects);
    }

    function setFlashMessage(message) {
        sessionStorage.setItem(getFlashKey(), message);
    }

    function completedCount(prospect) {
        return prospect.checklistItems.filter(function (item) { return item.isCompleted; }).length;
    }

    function totalCount(prospect) {
        return prospect.checklistItems.length;
    }

    function parseLocalDate(value) {
        if (!value) {
            return new Date();
        }

        return new Date(`${value}T00:00:00`);
    }

    function toDateTime(value) {
        return new Date(value);
    }

    function compareByNextContact(a, b) {
        return parseLocalDate(a.nextContactDate) - parseLocalDate(b.nextContactDate);
    }

    function getDashboard() {
        const prospects = loadProspects();
        const upcomingContacts = [...prospects]
            .sort(compareByNextContact)
            .slice(0, 5);

        const recentProspects = [...prospects]
            .sort(function (a, b) {
                return toDateTime(b.createdAt) - toDateTime(a.createdAt);
            })
            .slice(0, 6);

        const priorityContacts = [...prospects]
            .filter(function (prospect) { return prospect.status !== 4; })
            .sort(function (a, b) {
                const dateDiff = compareByNextContact(a, b);
                if (dateDiff !== 0) {
                    return dateDiff;
                }

                return b.temperature - a.temperature;
            })
            .slice(0, 5);

        return {
            prospects: prospects,
            totalProspects: prospects.length,
            hotProspects: prospects.filter(function (prospect) { return prospect.temperature === 3; }).length,
            warmProspects: prospects.filter(function (prospect) { return prospect.temperature === 2; }).length,
            coldProspects: prospects.filter(function (prospect) { return prospect.temperature === 1; }).length,
            customers: prospects.filter(function (prospect) { return prospect.status === 2 || prospect.status === 3; }).length,
            consultants: prospects.filter(function (prospect) { return prospect.status === 4; }).length,
            upcomingContacts: upcomingContacts,
            recentProspects: recentProspects,
            priorityContacts: priorityContacts
        };
    }

    function formatDate(value) {
        if (!value) {
            return "";
        }

        return new Intl.DateTimeFormat("pt-BR").format(parseLocalDate(value));
    }

    function formatDateTime(value) {
        if (!value) {
            return "";
        }

        return new Intl.DateTimeFormat("pt-BR", {
            day: "2-digit",
            month: "2-digit",
            hour: "2-digit",
            minute: "2-digit"
        }).format(toDateTime(value));
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;")
            .replaceAll("'", "&#39;");
    }

    document.addEventListener('DOMContentLoaded', function () {
        const draftForm = document.querySelector('[data-draft-form]');
        if (!draftForm) return;

        draftForm.querySelectorAll('input, textarea, select').forEach(function (field) {
            field.addEventListener('change', function () {
                markDraftChanged();
            });
        });
    });

    window.addEventListener('beforeunload', function (event) {
        if (hasUnsavedDraft && isUnsavedAlertEnabled()) {
            event.preventDefault();
            event.returnValue = '';
        }
    });

    window.MMNProspects = {
        statusLabels: statusLabels,
        temperatureLabels: temperatureLabels,
        defaultChecklist: defaultChecklist,
        getCurrentUserId: getCurrentUserId,
        loadProspects: loadProspects,
        saveProspects: saveProspects,
        getProspectById: getProspectById,
        upsertProspect: upsertProspect,
        updateChecklist: updateChecklist,
        deleteProspect: deleteProspect,
        setFlashMessage: setFlashMessage,
        getDashboard: getDashboard,
        normalizeChecklist: normalizeChecklist,
        normalizeProspect: normalizeProspect,
        completedCount: completedCount,
        totalCount: totalCount,
        formatDate: formatDate,
        formatDateTime: formatDateTime,
        parseLocalDate: parseLocalDate,
        escapeHtml: escapeHtml,
        markDraftChanged: markDraftChanged,
        markDraftSaved: markDraftSaved,
        loadSettings: loadSettings,
        isUnsavedAlertEnabled: isUnsavedAlertEnabled
    };
})(window);
