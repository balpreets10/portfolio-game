mergeInto(LibraryManager.library, {
    IsTouchSupportedJS: function() {
        // More accurate touch detection
        return ('ontouchstart' in window) || 
               (navigator.maxTouchPoints > 0) || 
               (navigator.msMaxTouchPoints > 0);
    }
});