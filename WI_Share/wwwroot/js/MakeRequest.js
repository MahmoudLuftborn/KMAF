export default class MakeRequest {
	constructor(url, method = 'get', headers = {}) {
		this.method = method;
		this.url = url;
		this.headers = headers;
	}
	setHeaders(httpRequest) {
		for (const header in this.headers) {
			httpRequest.setRequestHeader(header, this.headers[header]);
		}
	}
	send(data = null) {
		return new Promise((resolve, reject) => {
			const xmlHttpRequest = new XMLHttpRequest();
			xmlHttpRequest.open(this.method, this.url);
			this.setHeaders(xmlHttpRequest);
			xmlHttpRequest.onload = () => {
				if (xmlHttpRequest.status >= 200 &&
					xmlHttpRequest.status < 300) {
					resolve(xmlHttpRequest.response);
				}
				else {
					reject({
						status: xmlHttpRequest.status,
						statusText: xmlHttpRequest.statusText,
						body: xmlHttpRequest.response,
					});
				}
			};
			xmlHttpRequest.onerror = () => {
				reject({
					status: xmlHttpRequest.status,
					statusText: xmlHttpRequest.statusText,
					body: xmlHttpRequest.response,
				});
			};
			xmlHttpRequest.send(data);
		});
	}
}
