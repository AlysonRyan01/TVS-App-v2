import { HttpClient, HttpResponse } from "@angular/common/http";
import { inject, Service } from "@angular/core";
import { Observable } from "rxjs";
import { BaseResponse } from "../../../../shared/models/base-response.interface";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { environment } from "../../../../../environments/environment";

@Service()
export class DeliveryModalService {
    private _http = inject(HttpClient);

    registerDelivery(id: number): Observable<HttpResponse<Blob>> {
        return this._http.put(`${environment.apiUrl}/service-orders/${id}/delivery`, {}, {
            observe: 'response',
            responseType: 'blob',
        });
    }
}
