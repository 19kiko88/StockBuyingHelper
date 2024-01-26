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
      next: () => {
        window.alert('delete done.');
        this._loadingService.setLoading(false);
      },
      error: err => {
        window.alert(`error. ${err}`);
        this._loadingService.setLoading(false);
      }      
    });
  }

}
