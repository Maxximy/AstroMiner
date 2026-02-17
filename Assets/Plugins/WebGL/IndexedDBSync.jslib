mergeInto(LibraryManager.library, {
    SyncIndexedDB: function () {
        FS.syncfs(false, function (err) {
            if (err) {
                console.error("IndexedDB sync failed:", err);
            }
        });
    }
});
