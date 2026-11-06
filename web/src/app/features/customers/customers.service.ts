import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { BaseResponse } from "../../shared/models/base-response.interface";
import { environment } from "../../../environments/environment";
import { Customer, CustomerForm } from "./models/customer.model";
import { Address } from "./models/address.model";
import { CustomerSearchParams } from "./models/customer-search.model";
import { PaginatedResult } from "../../shared/models/paginated-result.interface";

@Injectable({ providedIn: 'root' })
export class CustomersService {
    private _http = inject(HttpClient);

    searchCustomers(name: string): Observable<BaseResponse<Customer[]>> {
        return this._http.get<BaseResponse<Customer[]>>(`${environment.apiUrl}/customers`, { params: { name } });
    }

    /** Grade de clientes: filtros combinados, mais recentes primeiro, paginada. */
    searchPaged(params: CustomerSearchParams): Observable<BaseResponse<PaginatedResult<Customer[]>>> {
        const query: Record<string, string> = {
            pageNumber: String(params.pageNumber),
            pageSize: String(params.pageSize),
        };
        if (params.name) query['name'] = params.name;
        if (params.phone) query['phone'] = params.phone;
        if (params.document) query['document'] = params.document;
        return this._http.get<BaseResponse<PaginatedResult<Customer[]>>>(`${environment.apiUrl}/customers/search`, { params: query });
    }

    createCustomer(form: CustomerForm): Observable<BaseResponse<Customer>> {
        return this._http.post<BaseResponse<Customer>>(`${environment.apiUrl}/customers`, form);
    }

    updateCustomer(id: number, form: CustomerForm): Observable<BaseResponse<Customer>> {
        return this._http.put<BaseResponse<Customer>>(`${environment.apiUrl}/customers/${id}`, form);
    }

    getAddress(cep: string): Observable<BaseResponse<Address>> {
        return this._http.get<BaseResponse<Address>>(`${environment.apiUrl}/addresses/${cep}`);
    }
}
