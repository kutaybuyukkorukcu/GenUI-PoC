import { AlertCircle, CheckCircle, Info } from 'lucide-react';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '../ui/card';

import { Button } from '../ui/button';

interface ConfirmationDialogProps {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  variant?: 'info' | 'warning' | 'danger';
  data?: Record<string, unknown>;
  onConfirm?: () => void;
  onCancel?: () => void;
  sendMessage?: (message: string) => void;
}

export const ConfirmationDialog = ({
  title,
  message,
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  variant = 'info',
  data,
  onConfirm,
  onCancel,
  sendMessage,
}: ConfirmationDialogProps) => {
  const handleConfirm = () => {
    if (sendMessage) {
      sendMessage('Confirm');
    }
    onConfirm?.();
  };

  const handleCancel = () => {
    if (sendMessage) {
      sendMessage('Cancel');
    }
    onCancel?.();
  };
  const getVariantStyles = () => {
    switch (variant) {
      case 'warning':
        return {
          icon: <AlertCircle className="h-5 w-5 text-yellow-600" />,
          iconBg: 'bg-yellow-100',
          buttonVariant: 'default' as const,
        };
      case 'danger':
        return {
          icon: <AlertCircle className="h-5 w-5 text-red-600" />,
          iconBg: 'bg-red-100',
          buttonVariant: 'destructive' as const,
        };
      default:
        return {
          icon: <Info className="h-5 w-5 text-blue-600" />,
          iconBg: 'bg-blue-100',
          buttonVariant: 'default' as const,
        };
    }
  };

  const styles = getVariantStyles();

  return (
    <Card className="w-full">
      <CardHeader>
        <CardTitle className="flex items-center gap-3">
          <div className={`rounded-full p-2 ${styles.iconBg}`}>{styles.icon}</div>
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <p className="text-sm leading-relaxed text-muted-foreground">{message}</p>

        {/* Display data if provided */}
        {data && Object.keys(data).length > 0 && (
          <div className="mt-4 rounded-lg border bg-muted/30 p-4">
            <h4 className="mb-3 text-sm font-semibold">Details:</h4>
            <dl className="space-y-2">
              {Object.entries(data).map(([key, value]) => (
                <div key={key} className="flex justify-between text-sm">
                  <dt className="font-medium text-muted-foreground">
                    {key.replace(/([A-Z])/g, ' $1').trim()}:
                  </dt>
                  <dd className="font-semibold">{String(value)}</dd>
                </div>
              ))}
            </dl>
          </div>
        )}
      </CardContent>
      <CardFooter className="flex gap-3">
        <Button variant="outline" className="flex-1" onClick={handleCancel}>
          {cancelText}
        </Button>
        <Button variant={styles.buttonVariant} className="flex-1" onClick={handleConfirm}>
          <CheckCircle className="mr-2 h-4 w-4" />
          {confirmText}
        </Button>
      </CardFooter>
    </Card>
  );
};
