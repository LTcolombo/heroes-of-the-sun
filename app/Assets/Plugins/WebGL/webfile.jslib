mergeInto(LibraryManager.library, {
  UploadFile: function(gameObjectNamePtr, methodNamePtr, acceptExtPtr) {
    const gameObjectName = UTF8ToString(gameObjectNamePtr);
    const methodName = UTF8ToString(methodNamePtr);
    const acceptExt = UTF8ToString(acceptExtPtr);

    const input = document.createElement('input');
    input.type = 'file';
    input.accept = acceptExt;
    input.onchange = function (e) {
      const file = e.target.files[0];
      if (!file) return;

      const reader = new FileReader();
      reader.onload = function () {
        const bytes = new Uint8Array(reader.result);
        let binary = "";
        for (let i = 0; i < bytes.length; i++) {
          binary += String.fromCharCode(bytes[i]);
        }
        const base64Data = btoa(binary);
        const payload = file.name + "|" + base64Data;
        SendMessage(gameObjectName, methodName, payload);
      };
      reader.readAsArrayBuffer(file);
    };
    input.click();
  }
});