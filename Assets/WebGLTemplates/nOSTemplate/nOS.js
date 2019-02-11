const nos = window.NOS.V1;

if (!"NOS" in window)
{
  alert('Not running in nOS browser!');
}

function nOSCall(jparams) {
  var api = JSON.parse(jparams);

  if (("name" in api) && ("config" in api) && ("reqid" in api))
  { 
    // api call with parameters
    nos[api.name](JSON.parse(api.config))
    .then((result) => { 
          result = typeof result !== "string"
        ? JSON.stringify(result)
        : result;
       postResult(api.reqid, result, false) })
    .catch((err) => { postResult(api.reqid, err.message, true) });
  } else if (("name" in api) && ("reqid" in api))
  { 
    // api call without parameters
    nos[api.name]()
    .then((result) => { 
          result = typeof result !== "string"
        ? JSON.stringify(result)
        : result;
       postResult(api.reqid, result, false) })
    .catch((err) => { postResult(api.reqid, err.message, true) });
  }
  else if ("reqid" in api)
  {
    postResult(api.reqid, 'Invalid API name', true);
  }
  else 
  {
    postResult('-1', 'Missing request ID', true);
  }
}

function postResult(id, res, state) {
      var msg = {
        requestId: id,
        resultData: res,
        errorState: state 
      };
      window.postMessage(msg, "*");
}
