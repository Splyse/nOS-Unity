mergeInto(LibraryManager.library, {

  // pass the API call name, parameters and request id via JSON string
  // { "name": "getAddress", "config": "{json string with parameters for call}", "reqid": "1" }
  nOSAPICall: function(jparams) { nOSCall(Pointer_stringify(jparams)) },

  // call this once at start
  StartEventListener: function() {
    window.addEventListener('message',function(event) {
      if ("requestId" in event.data)
      {
        var resp = JSON.stringify(event.data);
        console.log('jslib recv: ' + resp);
        SendMessage('nOSConnector', 'nOSResponseHandler', resp);
      }
    },false);
  }
});
