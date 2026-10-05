// Thin wrapper around the vendored @microsoft/signalr client (js/signalr.min.js).
// Connects straight from the browser to SmartX.Api's CommandHub, independent
// of this Blazor Server app's own SignalR circuit to the browser — two
// separate SignalR connections doing two separate jobs.
window.smartXCommandHub = {
    connection: null,

    start: function (dotNetRef, hubUrl) {
        if (this.connection) {
            return Promise.resolve();
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .build();

        this.connection.on("ReceiveCommandResult", function (result) {
            dotNetRef.invokeMethodAsync("OnCommandResult", result);
        });

        this.connection.on("ReceiveAlert", function (alert) {
            dotNetRef.invokeMethodAsync("OnAlert", alert);
        });

        this.connection.onreconnected(function () {
            dotNetRef.invokeMethodAsync("OnConnectionStateChanged", true);
        });
        this.connection.onreconnecting(function () {
            dotNetRef.invokeMethodAsync("OnConnectionStateChanged", false);
        });
        this.connection.onclose(function () {
            dotNetRef.invokeMethodAsync("OnConnectionStateChanged", false);
        });

        return this.connection.start()
            .then(function () {
                dotNetRef.invokeMethodAsync("OnConnectionStateChanged", true);
            })
            .catch(function (err) {
                console.error("SmartX command hub connection error:", err);
                dotNetRef.invokeMethodAsync("OnConnectionStateChanged", false);
            });
    },

    stop: function () {
        if (this.connection) {
            this.connection.stop();
            this.connection = null;
        }
    }
};
