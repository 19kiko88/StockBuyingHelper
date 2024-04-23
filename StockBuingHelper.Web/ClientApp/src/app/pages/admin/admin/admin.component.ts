import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import { AdminService } from 'src/app/core/http/admin.service';
import { LoadingService } from 'src/app/shared/components/loading/loading.service';

@Component({
  selector: 'app-admin',
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.css'
})
export class AdminComponent {

  constructor(
    private _adminService: AdminService,
    private _loadingService: LoadingService
    )
  {}

  DeleteVolumeDetail()
  {
    this._loadingService.setLoading(true);

    this._adminService.DeleteVolumeDetail().subscribe({
      next: (res) => {
        if (res.success)
        {
          window.alert('delete done.');
        }
        else
        {
          window.alert(`delete error. ${res.message}`);
        }        
        this._loadingService.setLoading(false);
      } 
      ,error: (err: HttpErrorResponse) => 
      {
        this._loadingService.setLoading(false);
      }      
    });
  }

  RefreshStockList()
  {
    this._loadingService.setLoading(true);

    this._adminService.RefreshStockList().subscribe({
      complete: () =>{
        window.alert('refresh StockList done.');
        this._loadingService.setLoading(false);
      }
      ,error: (err: HttpErrorResponse) => 
      {
        window.alert('refresh StockList fail.');
        this._loadingService.setLoading(false);
      }      
    });
  }

  RefreshRevenueInfo()
  {
    this._loadingService.setLoading(true);

    this._adminService.RefreshRevenueInfo().subscribe({
      complete: () =>{
        window.alert('refresh RevenueInfo done.');
        this._loadingService.setLoading(false);
      }
      ,error: (err: HttpErrorResponse) => 
      {
        window.alert('refresh RevenueInfo fail.');
        this._loadingService.setLoading(false);
      }      
    });
  }

  RefreshVolumeInfo()
  {
    this._loadingService.setLoading(true);

    this._adminService.RefreshVolumeInfo().subscribe({
      complete: () =>{
        window.alert('refresh VolumeInfo done.');
        this._loadingService.setLoading(false);
      }
      ,error: (err: HttpErrorResponse) => 
      {
        window.alert('refresh VolumeInfo fail.');
        this._loadingService.setLoading(false);
      }      
    });
  }

  RefreshEpsInfo()
  {
    this._loadingService.setLoading(true);

    this._adminService.RefreshEpsInfo().subscribe({
      complete: () =>{
        window.alert('refresh EpsInfo done.');
        this._loadingService.setLoading(false);
      }
      ,error: (err: HttpErrorResponse) => 
      {
        window.alert('refresh EpsInfo fail.');
        this._loadingService.setLoading(false);
      }      
    });
  }

}
